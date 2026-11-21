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
        bool debugSession,debugOpen,debugGod,debugFreeze,debugFuel,debugNoEnemies,debugUnlocked;
        RouteMode routeMode;int debugTarget=1,debugDrop,debugTab;float nextDiagnostic;
        string authKeyInput="",authError="";
        StreamWriter diagnosticFile;string diagnosticPath,lastNetError;Phase loggedPhase=(Phase)(-1);int loggedEvent=-1,diagnosticSegment;
        readonly Queue<string> diagnosticTail=new Queue<string>();readonly Dictionary<int,string> playerFlags=new Dictionary<int,string>();
        Vector2 debugScroll;
        [Serializable] sealed class TraceRecord{public string utc,category,message;public int tick,localId;public bool host,sandbox;}
        string L(string vi,string en)=>EmberLanguage.English?en:vi;

        static bool VerifyConsoleAuth(string input)
        {
            if(string.IsNullOrEmpty(input))return false;
            try
            {
                byte[] salt=new byte[]{0x45,0x6d,0x62,0x65,0x72,0x56,0x6f,0x69,0x64,0x32,0x30,0x32,0x36};
                byte[] h=System.Text.Encoding.UTF8.GetBytes(input);
                using(var sha=System.Security.Cryptography.SHA256.Create())
                {
                    byte[] roundBuf=new byte[h.Length+salt.Length+1];
                    for(int i=0;i<2026;i++)
                    {
                        if(roundBuf.Length!=h.Length+salt.Length+1)roundBuf=new byte[h.Length+salt.Length+1];
                        Buffer.BlockCopy(h,0,roundBuf,0,h.Length);
                        Buffer.BlockCopy(salt,0,roundBuf,h.Length,salt.Length);
                        roundBuf[roundBuf.Length-1]=(byte)((i*7+13)&0xFF);
                        h=sha.ComputeHash(roundBuf);
                    }
                }
                byte[] expected=new byte[]{134,141,103,112,199,8,125,228,216,61,95,121,150,134,53,113,134,207,35,15,152,49,62,152,127,33,225,174,238,121,239,217};
                if(h.Length!=expected.Length)return false;
                int diff=0;
                for(int i=0;i<h.Length;i++)diff|=(h[i]^(expected[i]^0x5A));
                return diff==0;
            }
            catch{return false;}
        }

        void InitializeDiagnostics()
        {
            Application.SetStackTraceLogType(LogType.Log,StackTraceLogType.None);
            var args=Environment.GetCommandLineArgs();debugSession=args.Contains("--ember-debug")||args.Any(a=>a.StartsWith("--ember-smoke")||a.StartsWith("--ember-net-"));
            debugOpen=args.Contains("--ember-debug");
            debugUnlocked=args.Contains("--ember-debug");
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
            if((debugSession||debugGod||debugFreeze||debugFuel||debugNoEnemies)&&host&&World!=null){World.DebugEnabled=true;World.DebugInvulnerable=debugGod;World.DebugFreezeDread=debugFreeze;World.DebugInfiniteFuel=debugFuel;World.DebugNoEnemies=debugNoEnemies;}
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
        Profile SandboxProfile()=>new Profile{Ash=10000,Memory=1000,Tokens=1000,PerkSlots=3,Vessels=new[]{0,1,2,3,4,5},Perks=new[]{0,1,2,3,4,5,6},Recipes=Enumerable.Range(0,23).ToArray(),Hats=new[]{0,1,2,3},Pets=new[]{0,1,2,3}};
        void DebugDo(string action)
        {
            if(!host||World==null)return;World.DebugEnabled=true;if(World.DebugAction(action,debugTarget))Trace("debug.action",action+" target="+debugTarget);
        }
        void ApplyGodLoadout(Player p)
        {
            if(p==null||World==null)return;
            World.DebugEnabled=true;
            p.HasWeapon=true;
            p.Weapon=Weapon.ShadowBlades;
            p.HasSideWeapon=true;
            p.SideWeapon=Weapon.CrystalAxe;
            p.HasPickaxe=true;
            p.PickaxeTier=3;
            p.PickaxeDurability=200;
            p.CarriesLantern=true;
            p.Lantern=Lantern.Relic;
            p.LampOn=true;
            p.HP=p.MaxHP;
            p.Dread=0;
            p.Stamina=100;
            p.HeldSlot=1;
            heldSlot=1;

            if(p.Items!=null&&p.Items.Length>=19)
            {
                p.Items[0]=99;
                p.Items[1]=20;
                p.Items[2]=20;
                p.Items[3]=5;
                p.Items[4]=10;
                p.Items[5]=10;
                p.Items[6]=5;
                p.Items[14]=10;
                p.Items[15]=10;
                p.Items[16]=10;
                p.Items[17]=10;
                p.Items[18]=10;
            }
            if(p.Bag!=null&&p.Bag.Length>=5)
            {
                p.Bag[0]=500;
                p.Bag[1]=100;
                p.Bag[2]=100;
                p.Bag[3]=500;
                p.Bag[4]=500;
            }
            World.Publish();
            Trace("debug.god_loadout","equipped ShadowBlades + CrystalAxe + RelicLantern + PickaxeT3 + full items/resources");
        }
        void DrawAuthModal()
        {
            float modalW=Mathf.Min(430,Screen.width-32);
            float modalH=210;
            var rect=new Rect((Screen.width-modalW)/2,(Screen.height-modalH)/2-30,modalW,modalH);

            GUI.color=new Color(0,0,0,0.85f);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),white);
            GUI.color=Color.white;

            GUI.Box(rect,"");
            GUILayout.BeginArea(new Rect(rect.x+18,rect.y+16,rect.width-36,rect.height-32));

            GUILayout.Label(L("🔒 BẢO MẬT HỆ THỐNG — ADMIN CONSOLE","🔒 SYSTEM SECURITY — ADMIN CONSOLE"),title??label);
            GUILayout.Space(6);
            GUILayout.Label(L("Khu vực quản trị nội bộ. Vui lòng nhập mã bảo mật:","Restricted internal area. Enter authorization key:"),small??label);
            GUILayout.Space(8);

            GUI.SetNextControlName("AdminPasswordField");
            authKeyInput=GUILayout.PasswordField(authKeyInput??"",'*',32);

            if(!string.IsNullOrEmpty(authError))
            {
                var prevCol=GUI.color;
                GUI.color=new Color(1f,0.35f,0.35f);
                GUILayout.Label(authError,small??label);
                GUI.color=prevCol;
            }
            else
            {
                GUILayout.Space(12);
            }

            GUILayout.BeginHorizontal();
            bool submit=GUILayout.Button(L("Xác Nhận (Enter)","Unlock (Enter)"),GUILayout.Height(32));
            bool cancel=GUILayout.Button(L("Đóng (ESC)","Close (ESC)"),GUILayout.Height(32));
            GUILayout.EndHorizontal();

            if(Event.current.type==EventType.KeyDown&&(Event.current.keyCode==KeyCode.Return||Event.current.keyCode==KeyCode.KeypadEnter))
            {
                submit=true;
                Event.current.Use();
            }

            if(submit)
            {
                if(VerifyConsoleAuth(authKeyInput))
                {
                    debugUnlocked=true;
                    authKeyInput="";
                    authError="";
                }
                else
                {
                    authError=L("❌ Mã truy cập không chính xác!","❌ Invalid authorization key!");
                    authKeyInput="";
                }
            }
            if(cancel)
            {
                debugOpen=false;
                authKeyInput="";
                authError="";
            }

            GUILayout.EndArea();
            GUI.FocusControl("AdminPasswordField");
        }
        void DrawDebugPanel()
        {
            if(!debugOpen)return;InitStyles();GUI.depth=-100;
            if(!debugUnlocked)
            {
                DrawAuthModal();
                GUI.depth=0;
                return;
            }

            var box=new Rect(14,50,Mathf.Min(780,Screen.width-28),Mathf.Min(700,Screen.height-65));GUI.Box(box,"");
            GUILayout.BeginArea(new Rect(box.x+12,box.y+8,box.width-24,box.height-16));

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("🛠️ BẢNG TEST & SPAWN VẬT PHẨM — F8 ĐÓNG","🛠️ DEBUG & ITEM SPAWNER — F8 TO CLOSE"));
            if(UiButton(L("🔒 Khóa lại","🔒 Lock"),GUILayout.Width(100)))
            {
                debugUnlocked=false;
                debugOpen=false;
            }
            if(UiButton(L("Pivot bàn tay","Hand pivots"),GUILayout.Width(130)))
            {
                handPivotEditorOpen=!handPivotEditorOpen;
                if(handPivotEditorOpen&&Local!=null)handPivotSelected=(int)Local.Vessel;
            }
            GUILayout.EndHorizontal();

            if(handPivotEditorOpen)
            {
                handPivotScroll=GUILayout.BeginScrollView(handPivotScroll);
                DrawHandPivotEditor();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                GUI.depth=0;
                return;
            }

            string[] tabNames=new[]{
                L("⚔️ Trang Bị","⚔️ Weapons"),
                L("🎒 Túi Đồ & Item","🎒 Items"),
                L("🎩 Mũ & Pet","🎩 Hats & Pets"),
                L("⚡ Quyền Năng & Boss","⚡ Cheats"),
                L("🏛️ Lưu Trữ & Profile","🏛️ Save")
            };
            debugTab=GUILayout.Toolbar(debugTab,tabNames);

            debugScroll=GUILayout.BeginScrollView(debugScroll);

            var targetP=host&&World!=null?World.PlayerById(debugTarget)??World.PlayerById(localId):null;
            if(host&&World!=null&&State?.Players?.Length>1)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("Chọn mục tiêu:","Target:"),GUILayout.Width(80));
                var ids=State.Players.Select(p=>p.Id).ToArray();
                int idx=Math.Max(0,Array.IndexOf(ids,debugTarget));
                idx=GUILayout.Toolbar(idx,ids.Select(id=>"P"+id+(id==localId?" (Tôi)":"")).ToArray());
                debugTarget=ids[idx];
                GUILayout.EndHorizontal();
            }

            if(debugTab==0)
            {
                if(targetP==null)
                {
                    GUILayout.Label(L("⚠️ Chưa vào trận đấu! Bạn cần ở trong trận (Solo hoặc Host) để trang bị vũ khí.","⚠️ Not in a game run! Start a solo or host match to equip weapons."));
                    if(GUILayout.Button(L("⚡ BẮT ĐẦU TRẬN TEST (SANDBOX)","⚡ START TEST RUN (SANDBOX)"),GUILayout.Height(32)))EnableSandbox();
                }
                else
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label(L("🔥 NẠP ĐẦY TRANG BỊ MẠNH NHẤT (ONE-CLICK GOD LOADOUT):","🔥 ONE-CLICK GOD LOADOUT:"));
                    if(GUILayout.Button(L("⚡ TRANG BỊ TẬN RĂNG: Song Đao (Slot 1) + Rìu Tinh Thể (Slot 7) + Đèn Thánh Tích + Cuốc Cấp 3 + Full Đồ Tiêu Hao",
                                         "⚡ GOD LOADOUT: Shadow Blades (Slot 1) + Crystal Axe (Slot 7) + Relic Lantern + Tier 3 Pickaxe + Full Items"),GUILayout.Height(36)))
                    {
                        ApplyGodLoadout(targetP);
                    }
                    GUILayout.EndVertical();

                    GUILayout.Space(6);
                    GUILayout.Label(L("Vũ khí chính (Slot 1 / Phím 1) — [Đang cầm: "+targetP.Weapon+"]:",
                                      "Main Weapon (Slot 1 / Key 1) — [Equipped: "+targetP.Weapon+"]:"));
                    string[] wNames=new[]{
                        L("1. Gậy Gỗ (Stick)","1. Wooden Stick"),
                        L("2. Rìu Kiếm Khí (Crystal Axe)","2. Crystal Axe"),
                        L("3. Song Đao (Shadow Blades)","3. Shadow Blades"),
                        L("4. Nỏ Tinh Thể (Crossbow)","4. Crossbow"),
                        L("5. Súng Plasma (Lumen Gun)","5. Lumen Gun"),
                        L("6. Khiên Gương (Mirror Shield)","6. Mirror Shield"),
                        L("7. Cuốc Khai Khoáng (Pickaxe)","7. Pickaxe")
                    };
                    int selW=GUILayout.SelectionGrid((int)targetP.Weapon,wNames,2);
                    if(selW>=0&&selW!=(int)targetP.Weapon)
                    {
                        World.DebugEnabled=true;
                        targetP.HasWeapon=true;
                        targetP.Weapon=(Weapon)selW;
                        targetP.HeldSlot=1;
                        heldSlot=1;
                        World.Publish();
                    }

                    GUILayout.Space(6);
                    GUILayout.Label(L("Vũ khí phụ (Slot 7 / Phím 7) — [Đang gắn: "+targetP.SideWeapon+"]:",
                                      "Side Weapon (Slot 7 / Key 7) — [Equipped: "+targetP.SideWeapon+"]:"));
                    int selSideW=GUILayout.SelectionGrid((int)targetP.SideWeapon,wNames,2);
                    if(selSideW>=0&&selSideW!=(int)targetP.SideWeapon)
                    {
                        World.DebugEnabled=true;
                        targetP.HasSideWeapon=true;
                        targetP.SideWeapon=(Weapon)selSideW;
                        targetP.HeldSlot=7;
                        heldSlot=7;
                        World.Publish();
                    }

                    GUILayout.Space(6);
                    GUILayout.Label(L("Lồng Đèn (Lantern) — [Đang mang: "+(targetP.CarriesLantern?targetP.Lantern.ToString():"Chưa có")+"]:",
                                      "Lantern — [Carrying: "+(targetP.CarriesLantern?targetP.Lantern.ToString():"None")+"]:"));
                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button(L("Đèn Dầu (Oil)","Oil Lantern"))){World.DebugEnabled=true;targetP.CarriesLantern=true;targetP.Lantern=Lantern.Oil;targetP.LampOn=true;World.Publish();}
                    if(GUILayout.Button(L("Đèn Hỏa Ngục (Inferno)","Inferno Lantern"))){World.DebugEnabled=true;targetP.CarriesLantern=true;targetP.Lantern=Lantern.Inferno;targetP.LampOn=true;World.Publish();}
                    if(GUILayout.Button(L("Đèn Thánh Tích (Relic)","Relic Lantern"))){World.DebugEnabled=true;targetP.CarriesLantern=true;targetP.Lantern=Lantern.Relic;targetP.LampOn=true;World.Publish();}
                    GUILayout.EndHorizontal();

                    GUILayout.Space(6);
                    GUILayout.Label(L("Cuốc khai khoáng (Slot 8 / Phím 8) — [Cấp: "+targetP.PickaxeTier+" · Độ bền: "+targetP.PickaxeDurability+"/"+targetP.PickaxeMaxDurability+"]:",
                                      "Pickaxe (Slot 8 / Key 8) — [Tier: "+targetP.PickaxeTier+" · Durability: "+targetP.PickaxeDurability+"/"+targetP.PickaxeMaxDurability+"]:"));
                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button(L("Cuốc Đá (T0)","Stone (T0)"))){World.DebugEnabled=true;targetP.HasPickaxe=true;targetP.PickaxeTier=0;targetP.PickaxeDurability=targetP.PickaxeMaxDurability;targetP.HeldSlot=8;heldSlot=8;World.Publish();}
                    if(GUILayout.Button(L("Cuốc Sắt (T1)","Iron (T1)"))){World.DebugEnabled=true;targetP.HasPickaxe=true;targetP.PickaxeTier=1;targetP.PickaxeDurability=targetP.PickaxeMaxDurability;targetP.HeldSlot=8;heldSlot=8;World.Publish();}
                    if(GUILayout.Button(L("Cuốc Pha Lê (T2)","Crystal (T2)"))){World.DebugEnabled=true;targetP.HasPickaxe=true;targetP.PickaxeTier=2;targetP.PickaxeDurability=targetP.PickaxeMaxDurability;targetP.HeldSlot=8;heldSlot=8;World.Publish();}
                    if(GUILayout.Button(L("Cuốc Hắc Ám (T3)","Shadow (T3)"))){World.DebugEnabled=true;targetP.HasPickaxe=true;targetP.PickaxeTier=3;targetP.PickaxeDurability=targetP.PickaxeMaxDurability;targetP.HeldSlot=8;heldSlot=8;World.Publish();}
                    if(GUILayout.Button(L("Sửa 100%","Repair 100%"))){World.DebugEnabled=true;targetP.PickaxeDurability=targetP.PickaxeMaxDurability;World.Publish();}
                    GUILayout.EndHorizontal();
                }
            }
            else if(debugTab==1)
            {
                if(targetP==null)
                {
                    GUILayout.Label(L("⚠️ Chưa vào trận đấu! Bạn cần ở trong trận để nạp tài nguyên vào túi đồ.","⚠️ Not in a game run! Enter match to add resources to backpack."));
                    if(GUILayout.Button(L("⚡ BẮT ĐẦU TRẬN TEST (SANDBOX)","⚡ START TEST RUN (SANDBOX)"),GUILayout.Height(32)))EnableSandbox();
                }
                else
                {
                    GUILayout.Label(L("Tài nguyên trong túi (Bag) — Tải trọng: "+targetP.Load+"/"+targetP.Capacity+":",
                                      "Backpack Resources — Load: "+targetP.Load+"/"+targetP.Capacity+":"));
                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button(L("⚡ +100 TẤT CẢ TÀI NGUYÊN","⚡ +100 ALL RESOURCES"))){World.DebugEnabled=true;for(int i=0;i<5;i++)targetP.Bag[i]+=100;World.Publish();}
                    if(GUILayout.Button(L("Đầy Túi (999)","Fill 999"))){World.DebugEnabled=true;for(int i=0;i<5;i++)targetP.Bag[i]=999;World.Publish();}
                    if(GUILayout.Button(L("Xóa Trắng Túi","Clear Bag"))){World.DebugEnabled=true;for(int i=0;i<5;i++)targetP.Bag[i]=0;World.Publish();}
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button(L("+100 Bụi Sao ("+targetP.Bag[0]+")","+100 Dust ("+targetP.Bag[0]+")"))){World.DebugEnabled=true;targetP.Bag[0]+=100;World.Publish();}
                    if(GUILayout.Button(L("+50 Lõi Ánh Sáng ("+targetP.Bag[1]+")","+50 Core ("+targetP.Bag[1]+")"))){World.DebugEnabled=true;targetP.Bag[1]+=50;World.Publish();}
                    if(GUILayout.Button(L("+50 Mảnh Hư Không ("+targetP.Bag[2]+")","+50 Umbra ("+targetP.Bag[2]+")"))){World.DebugEnabled=true;targetP.Bag[2]+=50;World.Publish();}
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button(L("+100 Gỗ Cổ Thụ ("+targetP.Bag[3]+")","+100 Wood ("+targetP.Bag[3]+")"))){World.DebugEnabled=true;targetP.Bag[3]+=100;World.Publish();}
                    if(GUILayout.Button(L("+100 Quặng Tinh Thể ("+targetP.Bag[4]+")","+100 Ore ("+targetP.Bag[4]+")"))){World.DebugEnabled=true;targetP.Bag[4]+=100;World.Publish();}
                    GUILayout.EndHorizontal();

                    GUILayout.Space(8);
                    GUILayout.Label(L("Vật phẩm tiêu hao & Đồ đặt (Consumables & Deployables):","Consumables & Deployables:"));
                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button(L("⚡ +10 TẤT CẢ ĐỒ TIÊU HAO","⚡ +10 ALL CONSUMABLES"))){
                        World.DebugEnabled=true;
                        targetP.Items[0]+=10;targetP.Items[1]+=10;targetP.Items[2]+=10;targetP.Items[3]+=5;
                        targetP.Items[4]+=5;targetP.Items[5]+=5;targetP.Items[6]+=5;
                        targetP.Items[14]+=10;targetP.Items[15]+=10;targetP.Items[16]+=10;targetP.Items[17]+=10;targetP.Items[18]+=10;
                        World.Publish();
                    }
                    if(GUILayout.Button(L("Xóa Hết Đồ","Clear Items"))){World.DebugEnabled=true;for(int i=0;i<targetP.Items.Length;i++)targetP.Items[i]=0;World.Publish();}
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button(L("+10 Đuốc ("+targetP.Items[0]+")","+10 Torch ("+targetP.Items[0]+")"))){World.DebugEnabled=true;targetP.Items[0]+=10;World.Publish();}
                    if(GUILayout.Button(L("+10 Pháo Sáng ("+targetP.Items[1]+")","+10 Flare ("+targetP.Items[1]+")"))){World.DebugEnabled=true;targetP.Items[1]+=10;World.Publish();}
                    if(GUILayout.Button(L("+10 An Thần ("+targetP.Items[2]+")","+10 Sedative ("+targetP.Items[2]+")"))){World.DebugEnabled=true;targetP.Items[2]+=10;World.Publish();}
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button(L("+5 Đá Hồi Sinh ("+targetP.Items[3]+")","+5 Revival ("+targetP.Items[3]+")"))){World.DebugEnabled=true;targetP.Items[3]+=5;World.Publish();}
                    if(GUILayout.Button(L("+5 Tường Đá ("+targetP.Items[4]+")","+5 Wall ("+targetP.Items[4]+")"))){World.DebugEnabled=true;targetP.Items[4]+=5;World.Publish();}
                    if(GUILayout.Button(L("+5 Bẫy Chông ("+targetP.Items[5]+")","+5 Trap ("+targetP.Items[5]+")"))){World.DebugEnabled=true;targetP.Items[5]+=5;World.Publish();}
                    if(GUILayout.Button(L("+5 Rương ("+targetP.Items[6]+")","+5 Chest ("+targetP.Items[6]+")"))){World.DebugEnabled=true;targetP.Items[6]+=5;World.Publish();}
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button(L("+10 Nước Tăng Lực ("+targetP.Items[14]+")","+10 Energy ("+targetP.Items[14]+")"))){World.DebugEnabled=true;targetP.Items[14]+=10;World.Publish();}
                    if(GUILayout.Button(L("+10 Táo Rừng ("+targetP.Items[15]+")","+10 Apple ("+targetP.Items[15]+")"))){World.DebugEnabled=true;targetP.Items[15]+=10;World.Publish();}
                    if(GUILayout.Button(L("+10 Nấm Rừng ("+targetP.Items[16]+")","+10 Mushroom ("+targetP.Items[16]+")"))){World.DebugEnabled=true;targetP.Items[16]+=10;World.Publish();}
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button(L("+10 Táo Nướng ("+targetP.Items[17]+")","+10 Roasted Apple ("+targetP.Items[17]+")"))){World.DebugEnabled=true;targetP.Items[17]+=10;World.Publish();}
                    if(GUILayout.Button(L("+10 Súp Nấm ("+targetP.Items[18]+")","+10 Stew ("+targetP.Items[18]+")"))){World.DebugEnabled=true;targetP.Items[18]+=10;World.Publish();}
                    GUILayout.EndHorizontal();
                }
            }
            else if(debugTab==2)
            {
                if(targetP!=null)
                {
                    GUILayout.Label(L("Đổi Thể Xác / Nhân Vật (Vessel) [Đang chọn: "+targetP.Vessel+"]:",
                                      "Switch Vessel [Current: "+targetP.Vessel+"]:"));
                    string[] vNames=new[]{
                        L("Nhà Lữ Hành (Traveler)","Traveler"),
                        L("Hàn Nguyệt (Moon Mender)","Moon Mender"),
                        L("Kẻ Đặt Bẫy (Bomb Trapper)","Bomb Trapper"),
                        L("Hộ Vệ Cổ Tự (Rune Warden)","Rune Warden"),
                        L("Bóng Ma Không Gian (Space Wraith)","Space Wraith"),
                        L("Bé Moone (Little Moone)","Little Moone")
                    };
                    int curV=(int)targetP.Vessel;
                    int selV=GUILayout.SelectionGrid(curV,vNames,3);
                    if(selV>=0&&selV!=curV)
                    {
                        World.DebugEnabled=true;
                        targetP.Vessel=(Vessel)selV;
                        targetP.MaxHP=100;
                        targetP.HP=100;
                        targetP.SkillCD=0;
                        World.Publish();
                        Trace("debug.vessel","vessel="+selV);
                    }

                    GUILayout.Space(6);
                    GUILayout.Label(L("Trang bị Mũ (Hat) [Đang đội: "+(targetP.Hat<0?L("Không mũ","None"):((Hat)targetP.Hat).ToString())+"]:",
                                      "Equip Hat [Current: "+(targetP.Hat<0?"None":((Hat)targetP.Hat).ToString())+"]:"));
                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button(L("Tháo Mũ","No Hat"))){World.DebugEnabled=true;targetP.Hat=-1;World.Publish();}
                    if(GUILayout.Button(L("Nón Lá (Straw)","Straw Hat"))){World.DebugEnabled=true;targetP.Hat=(int)Hat.Straw;World.Publish();}
                    if(GUILayout.Button(L("Mũ Phù Thủy (Witch)","Witch Hat"))){World.DebugEnabled=true;targetP.Hat=(int)Hat.Witch;World.Publish();}
                    if(GUILayout.Button(L("Mũ Nến (Candle)","Candle Hat"))){World.DebugEnabled=true;targetP.Hat=(int)Hat.Candle;World.Publish();}
                    if(GUILayout.Button(L("Mũ Hề (Jester)","Jester Hat"))){World.DebugEnabled=true;targetP.Hat=(int)Hat.Jester;World.Publish();}
                    GUILayout.EndHorizontal();

                    GUILayout.Space(6);
                    GUILayout.Label(L("Trang bị Thú Cưng (Pet) [Đang theo: "+(targetP.Pet<0?L("Không dắt pet","None"):((Pet)targetP.Pet).ToString())+"]:",
                                      "Equip Pet [Current: "+(targetP.Pet<0?"None":((Pet)targetP.Pet).ToString())+"]:"));
                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button(L("Bỏ Pet","No Pet"))){World.DebugEnabled=true;targetP.Pet=-1;World.Publish();}
                    if(GUILayout.Button(L("Đom Đóm (Firefly)","Firefly"))){World.DebugEnabled=true;targetP.Pet=(int)Pet.Firefly;World.Publish();}
                    if(GUILayout.Button(L("Dơi Đêm (Bat)","Bat"))){World.DebugEnabled=true;targetP.Pet=(int)Pet.Bat;World.Publish();}
                    if(GUILayout.Button(L("Mèo Pha Lê (CrystalCat)","Crystal Cat"))){World.DebugEnabled=true;targetP.Pet=(int)Pet.CrystalCat;World.Publish();}
                    if(GUILayout.Button(L("Bóng Ma Nhỏ (LittleGhost)","Little Ghost"))){World.DebugEnabled=true;targetP.Pet=(int)Pet.LittleGhost;World.Publish();}
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label(L("⚠️ Đang ở ngoài sảnh. Bạn có thể mở khóa toàn bộ Mũ & Pet trong tab 'Lưu Trữ & Profile'.","⚠️ In Lobby. Unlock all Hats & Pets under 'Save & Profile' tab."));
                }
            }
            else if(debugTab==3)
            {
                GUILayout.Label(L("Quyền năng bất tử & Gián đoạn (Host Cheat Toggles):","Host Cheat Toggles:"));
                GUILayout.BeginHorizontal();
                debugGod=GUILayout.Toggle(debugGod,L("Bất tử (God mode)","God mode"));
                debugFreeze=GUILayout.Toggle(debugFreeze,L("Khóa Dread (Không sợ)","Freeze dread"));
                debugFuel=GUILayout.Toggle(debugFuel,L("Vô hạn nhiên liệu đèn","Infinite fuel"));
                debugNoEnemies=GUILayout.Toggle(debugNoEnemies,L("Dừng AI & Quái","Freeze AI/spawn"));
                GUILayout.EndHorizontal();

                if(host&&World!=null)
                {
                    GUILayout.Space(6);
                    GUILayout.Label(L("Triệu hồi Boss Thế Giới (World Boss Instant Spawner):","World Boss Instant Spawner:"));
                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button(L("🎲 Boss Ngẫu Nhiên","🎲 Random Boss"))){World.TriggerWorldBossEvent(null,localId);debugOpen=false;}
                    if(GUILayout.Button(L("1 · Hư Không (Void)","1 · Void Sovereign"))){World.TriggerWorldBossEvent((int)BossType.VoidSovereign,localId);debugOpen=false;}
                    if(GUILayout.Button(L("2 · Tro Tàn (Cinder)","2 · Cinder Behemoth"))){World.TriggerWorldBossEvent((int)BossType.CinderBehemoth,localId);debugOpen=false;}
                    if(GUILayout.Button(L("3 · Nhật Thực (Eclipse)","3 · Eclipse Weaver"))){World.TriggerWorldBossEvent((int)BossType.EclipseWeaver,localId);debugOpen=false;}
                    GUILayout.EndHorizontal();

                    GUILayout.Space(6);
                    GUILayout.Label(L("Thao tác tình huống trận (World Actions):","World Actions:"));
                    string[] actions={"heal","beacon","eclipse","portal","blackout","enemy","boss"};
                    string[] vi={"Hồi Đầy Máu/Stamina","Về Trụ Hải Đăng","Kích Hoạt Eclipse 90s","Mở Cổng Về","Tắt Lửa (Blackout)","Sinh Quái Thường","Sinh Boss Thường"};
                    int chosen=GUILayout.SelectionGrid(-1,EmberLanguage.English?actions:vi,4);
                    if(chosen>=0)DebugDo(actions[chosen]);

                    GUILayout.Space(6);
                    GUILayout.Label(L("Mô phỏng trạng thái tâm trí (Dread Presets):","Dread Presets:"));
                    int preset=GUILayout.Toolbar(-1,new[]{L("Bình thường / Vùng sáng","Normal / Safe"),L("Nghi ngờ 35%","Suspicion 35%"),L("Méo mó 65%","Distortion 65%"),L("Điên loạn 95%","Madness 95%")});
                    if(preset==0){World.DebugEnabled=true;World.DebugScenario(0,debugTarget);debugFreeze=false;}
                    else if(preset>0){int scenario=new[]{0,3,1,2}[preset];World.DebugEnabled=true;World.DebugScenario(scenario,debugTarget);debugFreeze=true;}
                }
            }
            else if(debugTab==4)
            {
                GUILayout.Label(L("Tiền tệ & Tài nguyên tài khoản (Profile Save):","Currencies & Progression:"));
                GUILayout.BeginHorizontal();
                if(GUILayout.Button(L("+10.000 Tro Tàn (Ash)","+10,000 Ash"))){Profile.Ash+=10000;if(!debugSession)SaveProfile();}
                if(GUILayout.Button(L("+1.000 Ký Ức (Memory)","+1,000 Memory"))){Profile.Memory+=1000;if(!debugSession)SaveProfile();}
                if(GUILayout.Button(L("+1.000 Thẻ Gacha (Tokens)","+1,000 Tokens"))){Profile.Tokens+=1000;if(!debugSession)SaveProfile();}
                GUILayout.EndHorizontal();

                GUILayout.Space(6);
                GUILayout.Label(L("Mở khóa toàn bộ tính năng tài khoản:","Unlock Everything:"));
                GUILayout.BeginHorizontal();
                if(GUILayout.Button(L("🔓 MỞ KHÓA TẤT CẢ (Vessels, Mũ, Pet, Kỹ năng, Công thức)",
                                     "🔓 UNLOCK ALL (Vessels, Hats, Pets, Perks, Recipes)"),GUILayout.Height(34)))
                {
                    Profile.Vessels=new[]{0,1,2,3,4,5};
                    Profile.Hats=new[]{0,1,2,3};
                    Profile.Pets=new[]{0,1,2,3};
                    Profile.Perks=new[]{0,1,2,3,4,5,6};
                    Profile.PerkSlots=3;
                    Profile.Recipes=Enumerable.Range(0,23).ToArray();
                    if(!debugSession)SaveProfile();
                }
                if(GUILayout.Button(L("⚡ BẬT SANDBOX TRẬN MỚI (RAM)","⚡ START NEW SANDBOX (RAM)"),GUILayout.Height(34)))
                {
                    EnableSandbox();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                GUILayout.Label(L("Chuẩn đoán & Mạng (Diagnostics & Route):","Diagnostics & Network:"));
                GUILayout.Label("Route: "+(lan?.RouteName??"SOLO")+" | mode="+routeMode+" | players="+(State?.Players?.Length??0));
                GUI.enabled=!network;
                routeMode=(RouteMode)GUILayout.Toolbar((int)routeMode,new[]{"Auto P2P → Relay","Direct only","Relay only"});
                GUI.enabled=true;

                GUILayout.Label(L("Mất gói giả lập (máy này): ","Simulated packet loss (this process): ")+debugDrop+"%");
                debugDrop=Mathf.RoundToInt(GUILayout.HorizontalSlider(debugDrop,0,50));
                GUILayout.Label("TX "+(lan?.SentPackets??0)+" | RX "+(lan?.ReceivedPackets??0)+" | retries "+(lan?.Retries??0)+" | repairs "+(lan?.SnapshotRepairs??0)+" | dropped "+(lan?.DroppedPackets??0));

                GUILayout.BeginHorizontal();
                if(GUILayout.Button(L("Mở thư mục Log","Open log folder")))EmberFileDialog.Reveal(diagnosticPath);
                if(GUILayout.Button(L("Về Sanctuary / Rời phòng","Return to Sanctuary / Leave room"))){LeaveRoom();Trace("debug.hub","returned to sanctuary");}
                GUILayout.EndHorizontal();

                GUILayout.Space(6);
                GUILayout.Label(L("Nhật ký gần nhất (Diagnostic Tail):","Diagnostic Tail:"));
                foreach(string record in diagnosticTail)GUILayout.Label(record);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.depth=0;
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
