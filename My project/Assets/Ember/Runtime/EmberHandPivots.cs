using System;
using System.IO;
using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        [Serializable]
        sealed class HandPivot
        {
            public int Vessel;
            public int FrontX, FrontY, BackX, BackY;
        }

        [Serializable]
        sealed class HandPivotFile
        {
            public HandPivot[] Vessels;
        }

        HandPivot[] handPivots;
        bool handPivotEditorOpen;
        int handPivotSelected;
        Vector2 handPivotScroll;
        string handPivotStatus;
        string HandPivotPath=>Path.Combine(Application.persistentDataPath,"hand-pivots.json");
#if UNITY_EDITOR
        string ProjectHandPivotPath=>Path.Combine(Application.dataPath,"Resources/Ember/hand-pivots.json");
#endif

        static HandPivot DefaultHandPivot(int vessel)
        {
            if(vessel==(int)Vessel.Wanderer)
                return new HandPivot{Vessel=vessel,FrontX=14,FrontY=14,BackX=5,BackY=14};
            if(vessel==(int)Vessel.LittleMoone)
                return new HandPivot{Vessel=vessel,FrontX=17,FrontY=12,BackX=8,BackY=12};
            return new HandPivot{Vessel=vessel,FrontX=11,FrontY=14,BackX=11,BackY=14};
        }

        HandPivot HandPivotFor(Vessel vessel)
        {
            int index=(int)vessel;
            return handPivots[Mathf.Clamp(index,0,handPivots.Length-1)];
        }

        void ApplyHandPivotJson(string json)
        {
            if(string.IsNullOrWhiteSpace(json))return;
            var stored=JsonUtility.FromJson<HandPivotFile>(json);
            if(stored?.Vessels==null)return;
            foreach(var pivot in stored.Vessels)
            {
                if(pivot==null||pivot.Vessel<0||pivot.Vessel>=handPivots.Length)continue;
                pivot.FrontX=Mathf.Clamp(pivot.FrontX,0,40);
                pivot.BackX=Mathf.Clamp(pivot.BackX,0,40);
                pivot.FrontY=Mathf.Clamp(pivot.FrontY,-8,42);
                pivot.BackY=Mathf.Clamp(pivot.BackY,-8,42);
                handPivots[pivot.Vessel]=pivot;
            }
        }

        void LoadHandPivots()
        {
            handPivots=new HandPivot[Catalog.VesselNames.Length];
            for(int i=0;i<handPivots.Length;i++)handPivots[i]=DefaultHandPivot(i);
            try
            {
#if UNITY_EDITOR
                if(File.Exists(ProjectHandPivotPath))
                    ApplyHandPivotJson(File.ReadAllText(ProjectHandPivotPath));
                else if(File.Exists(HandPivotPath))
                    ApplyHandPivotJson(File.ReadAllText(HandPivotPath));
#else
                var bundled=Resources.Load<TextAsset>("Ember/hand-pivots");
                if(bundled!=null)ApplyHandPivotJson(bundled.text);
                if(File.Exists(HandPivotPath))ApplyHandPivotJson(File.ReadAllText(HandPivotPath));
#endif
            }
            catch(Exception ex)
            {
                handPivotStatus=ex.Message;
                Debug.LogWarning("Could not load hand pivots: "+ex.Message);
            }
        }

        void SwitchVesselForHandPivot(int vessel)
        {
            if(!host||World==null)return;
            var p=World.PlayerById(localId);
            if(p==null||(int)p.Vessel==vessel)return;
            p.Vessel=(Vessel)vessel;
            p.MaxHP=100;
            p.HP=Mathf.Min(p.HP>0?p.HP:p.MaxHP,p.MaxHP);
            p.SkillCD=0;
            animationState.Remove(p.Id);
            Trace("debug.pivot_vessel","vessel="+vessel);
        }

        void DrawHandPivotEditor()
        {
            bool previousChanged=GUI.changed;
            GUI.changed=false;
            int chosen=UiSelection(handPivotSelected,Catalog.VesselNames,3);
            bool picked=GUI.changed||chosen!=handPivotSelected;
            GUI.changed=previousChanged||picked;
            if(picked)
            {
                handPivotSelected=chosen;
                SwitchVesselForHandPivot(chosen);
            }
            var pivot=handPivots[handPivotSelected];
            UiText(L("Đơn vị: pixel trên lưới 48 px mỗi ô. X tính từ tâm, Y tính từ chân. Quay phải: tay phải cầm; quay trái: tay trái cầm (tự phản chiếu X).",
                "Units: pixels on a 48 px per tile grid. X from center, Y from feet. Facing right: right hand holds; facing left: left hand holds (X mirrors)."),small);
            if(!host&&Local!=null&&(int)Local.Vessel!=handPivotSelected)
                UiText(L("Chọn Bản Ngã đang chơi để xem thay đổi ngay trong trận.","Choose the current vessel to preview changes in the match."),small);
            pivot.FrontX=HandPivotSlider(L("Tay cầm đồ · X","Holding hand · X"),pivot.FrontX,0,40);
            pivot.FrontY=HandPivotSlider(L("Tay cầm đồ · Y","Holding hand · Y"),pivot.FrontY,-8,42);
            pivot.BackX=HandPivotSlider(L("Tay còn lại · X","Off-hand · X"),pivot.BackX,0,40);
            pivot.BackY=HandPivotSlider(L("Tay còn lại · Y","Off-hand · Y"),pivot.BackY,-8,42);

            GUILayout.BeginHorizontal();
            if(UiButton(L("Theo nhân vật hiện tại","Use current vessel"))&&Local!=null)handPivotSelected=(int)Local.Vessel;
            if(UiButton(L("Khôi phục mặc định","Reset vessel")))handPivots[handPivotSelected]=DefaultHandPivot(handPivotSelected);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if(UiButton(L("Lưu JSON","Save JSON")))
            {
                try
                {
                    string json=JsonUtility.ToJson(new HandPivotFile{Vessels=handPivots},true);
                    byte[] bytes=System.Text.Encoding.UTF8.GetBytes(json);
                    EmberSave.WriteAtomic(HandPivotPath,bytes);
#if UNITY_EDITOR
                    Directory.CreateDirectory(Path.GetDirectoryName(ProjectHandPivotPath));
                    File.WriteAllBytes(ProjectHandPivotPath,bytes);
                    UnityEditor.AssetDatabase.Refresh();
                    handPivotStatus=L("Đã lưu pivot vào project (Assets/Resources/Ember/hand-pivots.json).","Pivots saved to project (Assets/Resources/Ember/hand-pivots.json).");
#else
                    handPivotStatus=L("Đã lưu pivot.","Pivots saved.");
#endif
                }
                catch(Exception ex){handPivotStatus=ex.Message;}
            }
            if(UiButton(L("Chép JSON","Copy JSON")))
            {
                GUIUtility.systemCopyBuffer=JsonUtility.ToJson(new HandPivotFile{Vessels=handPivots},true);
                handPivotStatus=L("Đã chép JSON.","JSON copied.");
            }
            if(UiButton(L("Mở thư mục","Open folder")))
            {
#if UNITY_EDITOR
                string revealPath=File.Exists(ProjectHandPivotPath)?ProjectHandPivotPath:HandPivotPath;
#else
                string revealPath=HandPivotPath;
#endif
                if(File.Exists(revealPath))EmberFileDialog.Reveal(revealPath);
            }
            GUILayout.EndHorizontal();
#if UNITY_EDITOR
            UiText(ProjectHandPivotPath,small);
#else
            UiText(HandPivotPath,small);
#endif
            if(!string.IsNullOrEmpty(handPivotStatus))UiText(handPivotStatus,small);
        }

        int HandPivotSlider(string title,int value,int minimum,int maximum)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title+"  "+value,GUILayout.Width(160));
            if(UiButton("−",GUILayout.Width(34)))value--;
            value=Mathf.RoundToInt(GUILayout.HorizontalSlider(Mathf.Clamp(value,minimum,maximum),minimum,maximum));
            if(UiButton("+",GUILayout.Width(34)))value++;
            GUILayout.EndHorizontal();
            return Mathf.Clamp(value,minimum,maximum);
        }
    }
}
