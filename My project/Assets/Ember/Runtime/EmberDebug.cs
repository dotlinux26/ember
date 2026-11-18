using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ember
{
    public sealed partial class EmberGame
    {
        bool debugSession,debugOpen,debugGod,debugFreeze,debugFuel,debugNoEnemies;
        RouteMode routeMode;int debugTarget=1,debugDrop;float nextDiagnostic;
        StreamWriter diagnosticFile;string diagnosticPath,lastNetError;Phase loggedPhase=(Phase)(-1);int loggedEvent=-1,diagnosticSegment;
        readonly Queue<string> diagnosticTail=new Queue<string>();readonly Dictionary<int,string> playerFlags=new Dictionary<int,string>();
        Vector2 debugScroll;
        [Serializable] sealed class TraceRecord{public string utc,category,message;public int tick,localId;public bool host,sandbox;}
        string L(string vi,string en)=>EmberLanguage.English?en:vi;
        void InitializeDiagnostics()
        {
            Application.SetStackTraceLogType(LogType.Log,StackTraceLogType.None);
            var args=Environment.GetCommandLineArgs();debugSession=args.Contains("--ember-debug")||args.Any(a=>a.StartsWith("--ember-smoke")||a.StartsWith("--ember-net-"));
            debugOpen=args.Contains("--ember-debug");
            if(args.Contains("--ember-route=direct"))routeMode=RouteMode.DirectOnly;if(args.Contains("--ember-route=relay"))routeMode=RouteMode.RelayOnly;
            string directory=Path.Combine(Application.persistentDataPath,"Logs");Directory.CreateDirectory(directory);
            diagnosticPath=Path.Combine(directory,"ember_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff")+"_"+System.Diagnostics.Process.GetCurrentProcess().Id+".jsonl");
            diagnosticFile=new StreamWriter(diagnosticPath,false){AutoFlush=true};
            Application.logMessageReceived+=CaptureUnityError;Trace("session.start","version="+Application.version+" unity="+Application.unityVersion+" route="+routeMode);
        }
        void Trace(string category,string message)
        {
            string record=JsonUtility.ToJson(new TraceRecord{utc=DateTime.UtcNow.ToString("O"),category=category,message=message,tick=State?.Tick??0,localId=localId,host=host,sandbox=debugSession});
            WriteDiagnostic(record);diagnosticTail.Enqueue(category+" | "+message);while(diagnosticTail.Count>7)diagnosticTail.Dequeue();
            Debug.Log("EMBER_TRACE "+record);
        }
        void WriteDiagnostic(string record)
        {
            try{if(diagnosticFile==null)return;if(diagnosticFile.BaseStream.Length>5*1024*1024){diagnosticFile.Dispose();diagnosticFile=new StreamWriter(diagnosticPath+"."+(++diagnosticSegment),false){AutoFlush=true};}diagnosticFile.WriteLine(record);}catch(IOException){diagnosticFile=null;}
        }
        void CaptureUnityError(string message,string stack,LogType type)
        {
            if(type!=LogType.Exception&&type!=LogType.Error)return;
            WriteDiagnostic(JsonUtility.ToJson(new TraceRecord{utc=DateTime.UtcNow.ToString("O"),category="unity.error",message=message+"\n"+stack,tick=State?.Tick??0,localId=localId,host=host,sandbox=debugSession}));
        }
        void DiagnosticTick()
        {
            if(Press(Key.F8))debugOpen=!debugOpen;
            if(debugSession&&host&&World!=null){World.DebugEnabled=true;World.DebugInvulnerable=debugGod;World.DebugFreezeDread=debugFreeze;World.DebugInfiniteFuel=debugFuel;World.DebugNoEnemies=debugNoEnemies;}
            if(lan!=null){lan.DebugDropPercent=debugSession?debugDrop:0;if(lan.Error!=lastNetError){lastNetError=lan.Error;if(lastNetError!=null)Trace("network.error",lastNetError);}}
            if(State==null)return;
            if(loggedPhase!=State.Phase){loggedPhase=State.Phase;Trace("phase.changed",State.Phase+" fuel="+State.Fuel.ToString("0.0")+" charge="+State.Charge.ToString("0"));}
            if(loggedEvent!=State.EventTick&&!string.IsNullOrEmpty(State.Event)){loggedEvent=State.EventTick;Trace("world.event",State.Event);}
            foreach(var p in State.Players){string flags="safe="+p.Safe+" mad="+p.Mad+" ghost="+p.Ghost+" extracted="+p.Extracted;if(!playerFlags.TryGetValue(p.Id,out var before)||before!=flags){playerFlags[p.Id]=flags;Trace("player.state","id="+p.Id+" "+flags);}}
            if(clock>=nextDiagnostic){nextDiagnostic=clock+5;Trace("session.health","players="+State.Players.Length+" route="+(lan?.RouteName??"SOLO")+" tx="+(lan?.SentPackets??0)+" rx="+(lan?.ReceivedPackets??0)+" retries="+(lan?.Retries??0)+" rejected="+(lan?.RejectedPackets??0)+" pending="+(lan?.ReliablePending??0)+" stateAge="+(network&&!host?Math.Max(0,Time.realtimeSinceStartupAsDouble-lan.LastStateTime).ToString("0.00"):"0"));}
        }
        void EnableSandbox()
        {
            LeaveRoom();debugSession=true;Profile=SandboxProfile();selected=0;StartSolo();debugOpen=true;Trace("debug.enabled","isolated in-memory profile; real save writes disabled");
        }
        Profile SandboxProfile()=>new Profile{Ash=10000,Memory=1000,Tokens=1000,PerkSlots=3,Vessels=new[]{0,1,2,3,4},Perks=new[]{0,1,2,3,4,5,6},Recipes=Enumerable.Range(0,14).ToArray(),Hats=new[]{0,1,2,3},Pets=new[]{0,1,2,3}};
        void DebugDo(string action)
        {
            if(!debugSession||!host||World==null)return;World.DebugEnabled=true;if(World.DebugAction(action,debugTarget))Trace("debug.action",action+" target="+debugTarget);
        }
        void DrawDebugPanel()
        {
            if(!debugOpen)return;InitStyles();GUI.depth=-100;
            var box=new Rect(18,100,Mathf.Min(690,Screen.width-36),Mathf.Min(620,Screen.height-120));GUI.Box(box,"");
            GUILayout.BeginArea(new Rect(box.x+12,box.y+8,box.width-24,box.height-16));
            GUILayout.Label(L("DEBUG / SANDBOX — F8 đóng","DEBUG / SANDBOX — F8 to close"));
            if(UiButton(L("Pivot bàn tay","Hand pivots")))
            {
                handPivotEditorOpen=!handPivotEditorOpen;
                if(handPivotEditorOpen&&Local!=null)handPivotSelected=(int)Local.Vessel;
            }
            if(handPivotEditorOpen)
            {
                handPivotScroll=GUILayout.BeginScrollView(handPivotScroll);
                DrawHandPivotEditor();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                GUI.depth=0;
                return;
            }
            if(!debugSession){GUILayout.Label(L("Bật sandbox sẽ bắt đầu trận test mới; không ghi đè save thật.","Enable sandbox starts a new test run; real profile is not overwritten."));if(GUILayout.Button(L("BẬT SANDBOX / TRẬN MỚI","ENABLE SANDBOX / NEW RUN")))EnableSandbox();GUILayout.EndArea();GUI.depth=0;return;}
            debugScroll=GUILayout.BeginScrollView(debugScroll);
            GUILayout.Label(L("Profile thử trong RAM. Thoát game là bỏ. Host điều khiển trạng thái trận.","In-memory test profile. Discarded at exit. Host controls world changes."));
            GUILayout.Label("Route: "+(lan?.RouteName??"SOLO")+" | mode="+routeMode+" | players="+(State?.Players?.Length??0));
            GUILayout.Label(L("Đổi tuyến ở Sanctuary trước khi tạo/tham gia phòng:","Choose route in Sanctuary before creating/joining a room:"));
            GUI.enabled=!network;routeMode=(RouteMode)GUILayout.Toolbar((int)routeMode,new[]{"Auto P2P → Relay","Direct only","Relay only"});GUI.enabled=true;
            GUILayout.BeginHorizontal();if(GUILayout.Button(L("Mở log","Open logs")))EmberFileDialog.Reveal(diagnosticPath);if(GUILayout.Button(L("Về Sanctuary","Return to Sanctuary"))){LeaveRoom();Trace("debug.hub","returned to sandbox hub");}GUILayout.EndHorizontal();
            GUI.enabled=host&&World!=null;
            if(State?.Players?.Length>0){var ids=State.Players.Select(p=>p.Id).ToArray();int index=Math.Max(0,Array.IndexOf(ids,debugTarget));index=GUILayout.Toolbar(index,ids.Select(id=>"Player "+id).ToArray());debugTarget=ids[index];}
            int preset=GUILayout.Toolbar(-1,new[]{L("Nghi ngờ 35%","Suspicion 35%"),L("Méo mó 65%","Distortion 65%"),L("Điên 95%","Madness 95%")});
            if(preset>=0){int scenario=new[]{3,1,2}[preset];World.DebugEnabled=true;if(World.DebugScenario(scenario,debugTarget)){debugFreeze=true;Trace("debug.scenario","preset="+scenario+" target="+debugTarget);}}
            if(GUILayout.Button(L("Trở về bình thường / vùng sáng","Reset normal / safe light"))){World.DebugEnabled=true;World.DebugScenario(0,debugTarget);debugFreeze=false;Trace("debug.scenario","normal / safe");}
            GUILayout.BeginHorizontal();debugGod=GUILayout.Toggle(debugGod,L("Bất tử","God mode"));debugFreeze=GUILayout.Toggle(debugFreeze,L("Khóa Dread","Freeze dread"));debugFuel=GUILayout.Toggle(debugFuel,L("Vô hạn đèn","Infinite fuel"));debugNoEnemies=GUILayout.Toggle(debugNoEnemies,L("Dừng AI/spawn","Freeze AI/spawn"));GUILayout.EndHorizontal();
            string[] actions={"supplies","heal","ghost","beacon","eclipse","portal","blackout","enemy","boss"};string[] vi={"Tài nguyên","Hồi đầy","Hồn ma","Về trụ","Eclipse 90s","Mở cổng","Tắt lửa","Sinh quái","Sinh boss"};
            int chosen=GUILayout.SelectionGrid(-1,EmberLanguage.English?actions:vi,3);if(chosen>=0)DebugDo(actions[chosen]);
            if(Local!=null){int vessel=GUILayout.Toolbar((int)Local.Vessel,new[]{"Traveler","Moon","Trapper","Warden","Wraith","Little Moone"});if(vessel!=(int)Local.Vessel){var p=World.PlayerById(localId);p.Vessel=(Vessel)vessel;p.MaxHP=100;p.HP=p.MaxHP;p.SkillCD=0;Trace("debug.vessel","vessel="+vessel);}}
            if(Local!=null){int weapon=GUILayout.Toolbar((int)Local.Weapon,new[]{"Stick","Axe","Blades","Bow","Gun","Mirror","Pickaxe"});if(weapon!=(int)Local.Weapon){World.PlayerById(localId).Weapon=(Weapon)weapon;Trace("debug.weapon","weapon="+weapon);}}
            GUI.enabled=true;
            GUILayout.Label(L("Mất gói giả lập (máy này): ","Simulated packet loss (this process): ")+debugDrop+"%");debugDrop=Mathf.RoundToInt(GUILayout.HorizontalSlider(debugDrop,0,50));
            GUILayout.Label("TX "+(lan?.SentPackets??0)+" | RX "+(lan?.ReceivedPackets??0)+" | retries "+(lan?.Retries??0)+" | repairs "+(lan?.SnapshotRepairs??0)+" | dropped "+(lan?.DroppedPackets??0));
            foreach(string record in diagnosticTail)GUILayout.Label(record);
            GUILayout.EndScrollView();GUILayout.EndArea();GUI.depth=0;
        }
        void ConfigureDiagnosticScenario()
        {
            string arg=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("--ember-scenario="));if(arg==null||!debugSession||!host)return;
            if(State.Phase==Phase.Sanctuary)StartSolo();World.DebugEnabled=true;string value=arg.Substring("--ember-scenario=".Length);
            int scenario=value=="35"?3:value=="65"?1:value=="95"?2:0;
            if(new[]{"normal","35","65","95"}.Contains(value)){World.DebugScenario(scenario);debugFreeze=scenario!=0;}
            else World.DebugAction(value);
            debugNoEnemies=true;Trace("debug.cli_scenario",value);
        }
        void CloseDiagnostics(){Application.logMessageReceived-=CaptureUnityError;diagnosticFile?.Dispose();diagnosticFile=null;}
    }
}
