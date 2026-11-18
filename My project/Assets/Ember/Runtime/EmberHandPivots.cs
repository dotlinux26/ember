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

        static HandPivot DefaultHandPivot(int vessel)
        {
            bool moone=vessel==(int)Vessel.LittleMoone;
            return new HandPivot
            {
                Vessel=vessel,
                FrontX=moone?10:11,
                FrontY=moone?12:14,
                BackX=moone?11:11,
                BackY=moone?12:14
            };
        }

        HandPivot HandPivotFor(Vessel vessel)
        {
            int index=(int)vessel;
            return handPivots[Mathf.Clamp(index,0,handPivots.Length-1)];
        }

        void LoadHandPivots()
        {
            handPivots=new HandPivot[Catalog.VesselNames.Length];
            for(int i=0;i<handPivots.Length;i++)handPivots[i]=DefaultHandPivot(i);
            if(!File.Exists(HandPivotPath))return;
            try
            {
                var stored=JsonUtility.FromJson<HandPivotFile>(File.ReadAllText(HandPivotPath));
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
            p.MaxHP=vessel==3?140:vessel==1?90:vessel==4?85:100;
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
                    EmberSave.WriteAtomic(HandPivotPath,System.Text.Encoding.UTF8.GetBytes(json));
                    handPivotStatus=L("Đã lưu pivot.","Pivots saved.");
                }
                catch(Exception ex){handPivotStatus=ex.Message;}
            }
            if(UiButton(L("Chép JSON","Copy JSON")))
            {
                GUIUtility.systemCopyBuffer=JsonUtility.ToJson(new HandPivotFile{Vessels=handPivots},true);
                handPivotStatus=L("Đã chép JSON.","JSON copied.");
            }
            if(UiButton(L("Mở thư mục","Open folder"))&&File.Exists(HandPivotPath))EmberFileDialog.Reveal(HandPivotPath);
            GUILayout.EndHorizontal();
            UiText(HandPivotPath,small);
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
