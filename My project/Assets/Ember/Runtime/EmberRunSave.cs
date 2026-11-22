using System;
using System.IO;
using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        const float RunAutoSaveInterval=20f;
        RunSaveData savedRunPreview;
        float runAutoSaveClock=RunAutoSaveInterval;
        readonly object runSaveIoLock=new object();
        volatile bool runSaveInFlight;

        string RunSavePath=>Path.Combine(Application.persistentDataPath,"run.ember");

        bool CanSaveCurrentSoloRun()
        {
            if(netSmoke||!host||debugSession||saveBlocked||rewarded)return false;
            if(World==null||State==null)return false;
            bool soloHost=!network||((State.Players?.Length??0)==1&&(State.Disconnected==null||State.Disconnected.Length==0));
            if(!soloHost)return false;
            return State.Phase==Phase.Explore||State.Phase==Phase.Eclipse||State.Phase==Phase.Extraction;
        }

        void RefreshSavedRunPreview()
        {
            savedRunPreview=null;
            if(debugSession||!File.Exists(RunSavePath))return;
            try
            {
                string json=EmberSave.Unprotect(File.ReadAllBytes(RunSavePath),LocalPassword);
                var loaded=JsonUtility.FromJson<RunSaveData>(json);
                EmberWorld.ValidateRunSave(loaded);
                savedRunPreview=loaded;
            }
            catch(Exception ex)
            {
                Trace("runsave.load_error",ex.Message);
            }
        }

        void SaveActiveRun(bool showNotice=false,bool asyncWrite=false)
        {
            if(!CanSaveCurrentSoloRun())return;
            if(asyncWrite&&runSaveInFlight)return;
            try
            {
                var data=World.ExportRun(heldSlot);
                string json=JsonUtility.ToJson(data);
                string path=RunSavePath;
                string pwd=LocalPassword;
                savedRunPreview=data;
                runAutoSaveClock=RunAutoSaveInterval;
                if(asyncWrite)
                {
                    runSaveInFlight=true;
                    System.Threading.Tasks.Task.Run(()=>
                    {
                        try
                        {
                            byte[] protectedBytes=EmberSave.Protect(json,pwd);
                            lock(runSaveIoLock){EmberSave.WriteAtomic(path,protectedBytes);}
                        }
                        catch(Exception ex){Debug.LogWarning("Async run save failed: "+ex.Message);}
                        finally{runSaveInFlight=false;}
                    });
                }
                else
                {
                    byte[] protectedBytes=EmberSave.Protect(json,pwd);
                    lock(runSaveIoLock){EmberSave.WriteAtomic(path,protectedBytes);}
                }
                if(showNotice)notice=L("Đã lưu trận đang chơi (run.ember).","Saved current run (run.ember).");
                Trace("runsave.saved","tick="+State.Tick+" phase="+State.Phase+" charge="+State.Charge);
            }
            catch(Exception ex)
            {
                Trace("runsave.error",ex.Message);
                if(showNotice)notice=L("Lưu trận thất bại: ","Failed to save run: ")+ex.Message;
            }
        }

        void ClearSavedRun()
        {
            savedRunPreview=null;
            try
            {
                if(File.Exists(RunSavePath))File.Delete(RunSavePath);
                string prev=RunSavePath+".previous";
                if(File.Exists(prev))File.Delete(prev);
            }
            catch(Exception ex)
            {
                Trace("runsave.clear_error",ex.Message);
            }
        }

        void TickRunAutoSave(float dt)
        {
            if(menu||!CanSaveCurrentSoloRun())return;
            runAutoSaveClock-=dt;
            if(runAutoSaveClock<=0)
            {
                runAutoSaveClock=RunAutoSaveInterval;
                SaveActiveRun(false,asyncWrite:true);
            }
        }

        void ResumeSoloRun()
        {
            if(debugSession||!File.Exists(RunSavePath))
            {
                RefreshSavedRunPreview();
                notice=L("Không tìm thấy bản lưu trận dở.","No saved run found.");
                return;
            }
            try
            {
                string json=EmberSave.Unprotect(File.ReadAllBytes(RunSavePath),LocalPassword);
                var data=JsonUtility.FromJson<RunSaveData>(json);
                var restored=EmberWorld.RestoreRun(data,installationId);
                lan?.Leave();
                network=false;host=true;localId=1;
                World=restored;
                State=World.S;
                State.Sandbox=false;
                selectedDifficulty=State.Difficulty;
                var local=Local;
                if(local!=null)
                {
                    selected=Mathf.Clamp((int)local.Vessel,0,Catalog.VesselNames.Length-1);
                    heldSlot=data.HeldSlot>=1&&data.HeldSlot<=60?data.HeldSlot:Mathf.Clamp(local.HeldSlot,1,60);
                    local.HeldSlot=heldSlot;
                }
                else heldSlot=1;
                selectedItem=0;inventoryOpen=false;craftOpen=false;beaconDepositOpen=false;pauseConfirm=false;pauseSettings=false;
                rewarded=false;accumulator=0;renderedSeed=int.MinValue;snapshotSequence=0;
                path.Clear();animationState.Clear();ClearActions();
                runAutoSaveClock=RunAutoSaveInterval;
                menu=false;
                notice=L("Đã tiếp tục trận dở.","Resumed saved run.");
                Trace("runsave.resumed","seed="+State.Seed+" tick="+State.Tick+" phase="+State.Phase);
            }
            catch(Exception ex)
            {
                notice=L("Không đọc được trận dở: ","Could not load saved run: ")+ex.Message;
                Trace("runsave.resume_error",ex.Message);
                RefreshSavedRunPreview();
            }
        }

        void DrawSavedRunCard()
        {
            if(debugSession||savedRunPreview==null||savedRunPreview.Snapshot==null||savedRunPreview.Snapshot.Players==null||savedRunPreview.Snapshot.Players.Length==0)return;
            var s=savedRunPreview.Snapshot;
            var p=s.Players[0];
            int vIdx=Mathf.Clamp((int)p.Vessel,0,Catalog.VesselNames.Length-1);
            int dIdx=Mathf.Clamp((int)s.Difficulty,0,2);
            string diffName=(EmberLanguage.English?Catalog.DifficultyNamesEn:Catalog.DifficultyNamesVi)[dIdx];
            string phaseName=s.Phase==Phase.Eclipse?L("Nhật thực","Eclipse"):s.Phase==Phase.Extraction?L("Rút lui","Extraction"):L("Thám hiểm","Explore");
            string clockText=Mathf.FloorToInt(s.Time/60).ToString("00")+":"+Mathf.FloorToInt(s.Time%60).ToString("00");

            Rect box=GUILayoutUtility.GetRect(180,112,GUILayout.ExpandWidth(true));
            var card=new Rect(box.x+4,box.y,Mathf.Max(140,box.width-8),box.height);
            PixelPanel(card,Color.Lerp(Lilac,Plum,.16f));
            DrawSpritePreview(new Rect(card.x+8,card.y+8,48,48),Frame((Vessel)vIdx,"idle",(int)(Time.unscaledTime*6)));
            UiLabel(new Rect(card.x+62,card.y+8,card.width-70,24),
                L("Trận dở · ","Saved run · ")+Catalog.VesselNames[vIdx],label);
            UiLabel(new Rect(card.x+62,card.y+34,card.width-70,24),
                diffName+" · "+phaseName+" · "+clockText+" · "+L("Trụ ","Beacon ")+Mathf.FloorToInt(s.Charge)+"/"+Mathf.FloorToInt(s.ChargeTarget)+" · HP "+Mathf.CeilToInt(p.HP),small);

            float discardW=76;
            float resumeW=Mathf.Max(90,card.width-24-discardW);
            if(UiButton(new Rect(card.x+8,card.y+64,resumeW,36),L("Tiếp tục trận dở","Continue saved run")))ResumeSoloRun();
            if(UiButton(new Rect(card.x+16+resumeW,card.y+64,discardW,36),L("Xóa","Clear")))
            {
                ClearSavedRun();
                notice=L("Đã xóa bản lưu trận dở.","Cleared saved run.");
            }
            GUILayout.Space(8);
        }
    }
}
