using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ember
{
    // Scene entry point. Presentation is a client of the host simulation.
    public sealed partial class EmberGame:MonoBehaviour
    {
        [NonSerialized]public EmberWorld World;public Snapshot State;public Profile Profile;
        EmberLan lan;bool network,host=true,craftOpen,ranges=true,menu=true,rewarded,moba,saveBlocked;int localId=1;
        Camera cam;GameObject mapRoot,entityRoot;float accumulator,sendClock,joinClock;int renderedSeed=int.MinValue;byte[] currentTiles;
        readonly Dictionary<int,SpriteRenderer> visibleTrees=new Dictionary<int,SpriteRenderer>();
        readonly Stack<SpriteRenderer> treePool=new Stack<SpriteRenderer>();
        readonly Dictionary<int,V2> prevPlayerPos=new Dictionary<int,V2>(),prevEnemyPos=new Dictionary<int,V2>(),prevProjPos=new Dictionary<int,V2>();
        float renderInterp=1f;
        Vector2Int visibleChunk=new Vector2Int(int.MinValue,int.MinValue);
        readonly Dictionary<string,SpriteRenderer> renderers=new Dictionary<string,SpriteRenderer>();
        readonly Dictionary<string,Sprite[]> atlases=new Dictionary<string,Sprite[]>();
        readonly Dictionary<string,Sprite> primitives=new Dictionary<string,Sprite>();
        readonly Queue<V2> path=new Queue<V2>();
        V2 clickMarker;float clickMarkerUntil,mobaHoldClock;
        readonly List<Rect> hudBlockingRects=new List<Rect>();
        bool hudCapturing;
        void RegisterHudRect(Rect rect)
        {
            if(hudCapturing&&Event.current!=null&&Event.current.type==EventType.Repaint&&rect.width>4&&rect.height>4)
                hudBlockingRects.Add(rect);
        }
        bool IsMouseOverHud()
        {
            if(Mouse.current==null)return false;
            Vector2 mp=Mouse.current.position.ReadValue();
            Vector2 guiPos=new Vector2(mp.x,Screen.height-mp.y);
            if(new Rect(Screen.width-108,8,100,36).Contains(guiPos))return true;
            for(int i=0;i<hudBlockingRects.Count;i++)
                if(hudBlockingRects[i].Contains(guiPos))return true;
            return false;
        }
        readonly HashSet<string> touched=new HashSet<string>();
        string installationId;
        const string InstallationIdPref="Ember.PlayerId";
        const string DisplayNamePref="Ember.DisplayName";
        const string DifficultyPref="Ember.Difficulty";

        void InitializeIdentity()
        {
            nameText=PlayerPrefs.GetString(DisplayNamePref,L("Lữ khách","Traveler"));
            installationId=PlayerPrefs.GetString(InstallationIdPref,"");
            if(!Guid.TryParseExact(installationId,"N",out var id)||id==Guid.Empty)
            {
                installationId=Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(InstallationIdPref,installationId);
            }
            if(debugSession)installationId=Guid.NewGuid().ToString("N");
        }

        Command intent=new Command();string nameText="Lữ khách",address="127.0.0.1",password="ember",savePassword="",notice="",saveFile="";
        int selected,roomCapacity=4,snapshotSequence,netExpected=2;float clock;Vector2 scroll;GUIStyle title,label,small;Texture2D white;Material spriteMaterial;AudioSource audioSource;AudioClip tone,footstep;float phantomClock;bool netSmoke,netSmokeValidated;
        string roomName="Đội giữ lửa",brokerAddress="140.245.39.19",joinRoomCode="";bool readyWanted,usePublicBroker,fixedRoomCode;
        readonly Dictionary<int,(string clip,float began)> animationState=new Dictionary<int,(string,float)>();
        static readonly string[] Art={"lyra_moonscout","moon_mender","bomb_trapper","rune_warden","space_wraith","little_moone"};
        static readonly Color Cyan=new Color(.35f,.95f,.9f),Coral=new Color(1,.34f,.4f),Gold=new Color(1,.8f,.4f);

        void Awake()
        {
            InitializeDiagnostics();
            EmberLanguage.English=PlayerPrefs.GetInt("Ember.Language",0)==1;Application.runInBackground=true;QualitySettings.vSyncCount=0;
            cam=Camera.main;if(cam==null)cam=new GameObject("Ember camera").AddComponent<Camera>();
            cam.orthographic=true;cam.orthographicSize=10;cam.backgroundColor=new Color(.04f,.05f,.09f);cam.clearFlags=CameraClearFlags.SolidColor;
            entityRoot=new GameObject("Ember entities");mapRoot=new GameObject("Ember map");
            spriteMaterial=Resources.Load<Material>("Ember/EmberSprites");
            SetupLighting();
            audioSource=gameObject.AddComponent<AudioSource>();tone=MakeTone(440,.12f);footstep=MakeTone(90,.08f);
            InitializeSettings();
            LoadHandPivots();
            LoadQuickSlots();
            InitializeIdentity();
            if(FindAnyObjectByType<AudioListener>()==null)cam.gameObject.AddComponent<AudioListener>();
            white=new Texture2D(1,1);white.SetPixel(0,0,Color.white);white.Apply();
            selectedDifficulty=(Difficulty)Mathf.Clamp(PlayerPrefs.GetInt(DifficultyPref,(int)Difficulty.Normal),0,2);
            LoadProfile();selected=Profile.SelectedVessel;
            if(!Profile.CanPlay(selected))selected=0;
            RefreshSavedRunPreview();
            LoadLastCoopSession();
            NewWorld(Environment.TickCount);
            foreach(var arg in Environment.GetCommandLineArgs())
            {
                if(arg.StartsWith("--ember-broker=")){brokerAddress=arg.Substring(15);usePublicBroker=true;}
                if(arg.StartsWith("--ember-room=")){joinRoomCode=arg.Substring(13).ToUpperInvariant();fixedRoomCode=true;}
                if(arg.StartsWith("--ember-address="))address=arg.Substring(16);
            }
            foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("--ember-players="))int.TryParse(arg.Substring(16),out netExpected);
            if(Environment.GetCommandLineArgs().Contains("--ember-lang=en"))EmberLanguage.English=true;if(Environment.GetCommandLineArgs().Contains("--ember-lang=vi"))EmberLanguage.English=false;
            netExpected=Mathf.Clamp(netExpected,1,4);
            if(Environment.GetCommandLineArgs().Contains("--ember-smoke")){StartSolo();Invoke(nameof(SmokeCheck),3);}
            if(Environment.GetCommandLineArgs().Contains("--ember-net-host")){netSmoke=true;roomCapacity=netExpected;password="ember-loopback-test";HostGame();Invoke(nameof(NetSmokeCheck),35);}
            if(Environment.GetCommandLineArgs().Contains("--ember-net-client")){netSmoke=true;password="ember-loopback-test";JoinGame();Invoke(nameof(NetSmokeCheck),35);}
            ConfigureDiagnosticScenario();
        }
        void NetSmokeCheck()
        {
            if(netSmokeValidated)return;netSmokeValidated=true;CancelInvoke(nameof(NetSmokeCheck));
            bool ok=State!=null&&State.Phase==Phase.Explore&&State.Players.Length==netExpected&&State.Players.All(p=>p.Id==1||p.Pos.X>(p.Id-1)*1.5f+1);
            Debug.Log("EMBER_NET_SMOKE "+(ok?"PASS":"FAIL")+" host="+host+" expected="+netExpected+" players="+(State?.Players?.Length??0)+" phase="+State?.Phase);Invoke(nameof(SmokeQuit),5);
        }
        void SmokeCheck()
        {
            bool ok=World!=null&&State.Tick>30&&State.Players.Length==1&&renderers.Count>0&&atlases.Count>0;
            Debug.Log("EMBER_SMOKE "+(ok?"PASS":"FAIL")+" tick="+State.Tick+" sprites="+renderers.Count);
            ScreenCapture.CaptureScreenshot(Path.Combine(Application.persistentDataPath,"ember-smoke.png"));
            Invoke(nameof(SmokeQuit),1);
        }
        void SmokeQuit(){Application.Quit();}
        Difficulty selectedDifficulty=Difficulty.Normal;
        void NewWorld(int seed)
        {
            World=new EmberWorld(seed,250,selectedDifficulty){DebugEnabled=debugSession};World.S.Capacity=roomCapacity;World.S.RoomName=roomName;World.AddPlayer(1,nameText,(Vessel)selected,Profile.EquippedPerks.Take(Profile.PerkSlots).ToArray(),Profile.SelectedHat,Profile.SelectedPet,Profile.PerkRanks,Profile.Recipes,installationId);
            heldSlot=1;selectedItem=0;inventoryOpen=false;pauseConfirm=false;pauseSettings=false;
            var hostPlayer=World.PlayerById(1);if(hostPlayer!=null)hostPlayer.HeldSlot=1;
            State=World.S;State.Sandbox=debugSession;rewarded=false;accumulator=0;renderedSeed=int.MinValue;snapshotSequence=0;path.Clear();animationState.Clear();prevPlayerPos.Clear();prevEnemyPos.Clear();prevProjPos.Clear();enemyFacingLeft.Clear();ClearActions();
        }
        void StartSolo(){lan?.Leave();network=false;host=true;localId=1;ClearSavedRun();NewWorld(Environment.TickCount);World.S.Capacity=1;World.StartRun();runAutoSaveClock=RunAutoSaveInterval;menu=false;SaveActiveRun(false,asyncWrite:true);}
        void HostGame()
        {
            try{lan?.Leave();lan=new EmberLan{Capacity=roomCapacity,Mode=routeMode,Trace=Trace};
                if(usePublicBroker){const string chars="ABCDEFGHJKLMNPQRSTUVWXYZ23456789";var bytes=EmberCrypto.Random(6);if(!fixedRoomCode)joinRoomCode=new string(bytes.Select(b=>chars[b%chars.Length]).ToArray());lan.OpenPublic(true,brokerAddress,joinRoomCode,password);}else lan.Open(true,"",password);
                network=true;host=true;localId=1;NewWorld(Environment.TickCount);
                lan.CanRejoin=json=>{try{var info=JsonUtility.FromJson<JoinInfo>(json);return info!=null&&!(info.Sandbox&&!debugSession)&&World!=null&&World.CanRejoin(info.PlayerId);}catch{return false;}};
                lan.Joined=(id,json)=>{var info=JsonUtility.FromJson<JoinInfo>(json);if(info==null||(info.Sandbox&&!debugSession))throw new InvalidDataException();if(World.CanRejoin(info.PlayerId)){World.RejoinPlayer(id,info.PlayerId);return;}World.AddPlayer(id,info.Name,(Vessel)Mathf.Clamp(info.Vessel,0,Catalog.VesselNames.Length-1),(info.Perks??Array.Empty<int>()).Where(p=>p>=0&&p<7).Distinct().Take(3).ToArray(),Mathf.Clamp(info.Hat,-1,3),Mathf.Clamp(info.Pet,-1,3),(info.Ranks??new int[7]).Take(7).Select(r=>Mathf.Clamp(r,0,3)).ToArray(),(info.Recipes??Enumerable.Range(0,7).ToArray()).Where(r=>r>=0&&r<14).Distinct().Take(14).ToArray(),info.PlayerId);};
                lan.Left=id=>World.RemovePlayer(id);lan.Input=(id,json)=>World.Submit(id,JsonUtility.FromJson<Command>(json));
                lan.ReadyChanged=(id,ready)=>World.SetReady(id,ready);
                menu=false;notice="Phòng đã mở. Có thể bắt đầu với 1 người hoặc chờ bạn bè.";
            }catch(Exception ex){notice=ex.Message;}
        }
        void JoinGame()
        {
            try{lan?.Leave();lan=new EmberLan{Mode=routeMode,Trace=Trace};
                if(usePublicBroker){joinRoomCode=joinRoomCode.Trim().ToUpperInvariant();if(joinRoomCode.Length!=6)throw new ArgumentException("Mã phòng gồm 6 ký tự.");lan.OpenPublic(false,brokerAddress,joinRoomCode,password);}else lan.Open(false,address,password);
                network=true;host=false;localId=0;menu=false;State=null;readyWanted=false;
                SaveLastCoopSession();
                lan.State=json=>{var snapshot=JsonUtility.FromJson<Snapshot>(json);if(snapshot?.Players!=null){if(snapshot.Sandbox&&!debugSession){debugSession=true;Profile=SandboxProfile();Trace("debug.remote_sandbox","host test room: local profile writes disabled");}bool firstState=State==null;State=snapshot;localId=lan.LocalId;if(firstState&&Local!=null&&Local.HeldSlot>=1&&Local.HeldSlot<=8)heldSlot=Local.HeldSlot;}};
                joinClock=0;notice="Đang kết nối LAN/VPN...";
            }catch(Exception ex){notice=ex.Message;}
        }
        [Serializable] sealed class JoinInfo{public string Name,PlayerId;public bool Sandbox;public int Vessel,Hat=-1,Pet=-1;public int[] Perks,Ranks,Recipes;}
        void CapturePrevPositions()
        {
            if(State==null)return;
            prevPlayerPos.Clear();
            if(State.Players!=null)foreach(var p in State.Players)prevPlayerPos[p.Id]=p.Pos;
            prevEnemyPos.Clear();
            if(State.Enemies!=null)foreach(var e in State.Enemies)prevEnemyPos[e.Id]=e.Pos;
            prevProjPos.Clear();
            if(State.Projectiles!=null)foreach(var s in State.Projectiles)prevProjPos[s.Id]=s.Pos;
        }
        static V2 SmoothPos(V2 prev,V2 curr,float t)
        {
            if(V2.Distance(prev,curr)>=3.5f)return curr;
            return new V2(Mathf.Lerp(prev.X,curr.X,t),Mathf.Lerp(prev.Y,curr.Y,t));
        }
        V2 VisualPlayerPos(Player p)=>prevPlayerPos.TryGetValue(p.Id,out var prev)?SmoothPos(prev,p.Pos,renderInterp):p.Pos;
        V2 VisualEnemyPos(Enemy e)=>prevEnemyPos.TryGetValue(e.Id,out var prev)?SmoothPos(prev,e.Pos,renderInterp):e.Pos;
        V2 VisualProjectilePos(Projectile s)=>prevProjPos.TryGetValue(s.Id,out var prev)?SmoothPos(prev,s.Pos,renderInterp):s.Pos;

        void Update()
        {
            clock+=Time.deltaTime;lan?.Poll(Time.realtimeSinceStartupAsDouble);DiagnosticTick();
            TickCoopAutoReconnect(Time.deltaTime);
            if(network&&!host&&lan.LocalId==0&&string.IsNullOrEmpty(lan.Error)){joinClock-=Time.deltaTime;if(joinClock<=0){lan.Join(JsonUtility.ToJson(new JoinInfo{Name=nameText,PlayerId=installationId,Sandbox=debugSession,Vessel=selected,Perks=Profile.EquippedPerks.Take(Profile.PerkSlots).ToArray(),Ranks=Profile.PerkRanks,Recipes=Profile.Recipes,Hat=Profile.SelectedHat,Pet=Profile.SelectedPet}));joinClock=1;}}
            SampleFrameRate();
            if(Local!=null&&heldSlot>=2&&heldSlot<=5&&Local.Items[heldSlot-2]<=0)heldSlot=6;
            ReadInput();
            ReadCameraZoom();
            if(netSmoke&&!host){intent.X=State?.Phase==Phase.Explore&&State.Time<6?1:0;intent.Y=0;if(lan.LocalId>0&&!readyWanted){readyWanted=true;lan.SetReady(true);}}
            if(netSmoke&&host&&State.Players.Length==netExpected&&State.Players.All(p=>p.Ready))BeginRoomRun();
            if((!menu||network)&&host)
            {
                accumulator+=Mathf.Min(Time.deltaTime,.1f);
                while(accumulator>=EmberWorld.Step){CapturePrevPositions();World.Submit(localId,intent);World.Tick();accumulator-=EmberWorld.Step;ClearActions();}
                State=World.S;
                renderInterp=Mathf.Clamp01(accumulator/EmberWorld.Step);
                sendClock-=Time.deltaTime;if(network&&sendClock<=0){lan.Broadcast(JsonUtility.ToJson(State),++snapshotSequence);sendClock=.1f;}
            }
            else if(network&&!host)
            {
                renderInterp=1f;
                sendClock-=Time.deltaTime;if(sendClock<=0){if(intent.HasActions&&State?.Phase!=Phase.Sanctuary)lan.SendAction(JsonUtility.ToJson(intent));lan.SendInput(JsonUtility.ToJson(new Command{X=intent.X,Y=intent.Y}));sendClock=1f/30;ClearActions();}
            }
            else renderInterp=1f;
            if(State==null)return;
            if(renderedSeed!=State.Seed)BuildMap();
            Render();
            RenderLighting();
            UpdateAmbientAudio();
            StreamTrees();
            if(netSmoke&&!netSmokeValidated&&State.Tick>120&&State.Players.Length==netExpected&&State.Players.All(p=>p.Id==1||p.Pos.X>(p.Id-1)*1.5f+1))NetSmokeCheck();
            if(Local!=null&&Local.Dread>=30&&!Local.Safe){phantomClock-=Time.deltaTime;if(phantomClock<=0){audioSource.panStereo=Mathf.Sin(clock*2)*.8f;audioSource.PlayOneShot(footstep,.12f);phantomClock=3.5f;}}
            if((Local?.Extracted==true||(Local?.Ghost==true&&State.TeamExtracted>0)||State.Phase==Phase.Won||State.Phase==Phase.Lost)&&!rewarded)
            {
                var p=Local;if(p!=null)RecordRunReward(p);
            }
            TickRunAutoSave(Time.deltaTime);
        }
        int spectateId;
        Player Local=>State?.Players?.FirstOrDefault(p=>p.Id==localId);
        Player[] SpectateCandidates()=>State?.Players?.Where(q=>q.Id!=localId&&!q.Ghost&&!q.Extracted).OrderBy(q=>q.PartySlot).ToArray()??Array.Empty<Player>();
        Player CameraTarget()
        {
            var local=Local;
            if(local==null)return null;
            if(!local.Ghost&&!local.Extracted){spectateId=0;return local;}
            var alive=SpectateCandidates();
            if(alive.Length==0)return local;
            var picked=alive.FirstOrDefault(q=>q.Id==spectateId);
            if(picked!=null)return picked;
            spectateId=alive[0].Id;
            return alive[0];
        }
        void CycleSpectate(int delta)
        {
            var alive=SpectateCandidates();
            if(alive.Length==0)return;
            int index=Array.FindIndex(alive,q=>q.Id==spectateId);
            if(index<0)index=0;
            spectateId=alive[(index+delta+alive.Length)%alive.Length].Id;
        }
        void BeginRoomRun(){if(World.StartRun(true)){lan.AcceptingJoins=false;ClearActions();}}
        void LeaveRoom(){coopReconnectArmed=false;try{lan?.Leave();}catch(Exception ex){Debug.LogWarning(ex.Message);}network=false;host=true;localId=1;menu=true;State=null;path.Clear();ClearActions();}
        static AudioClip MakeTone(float hz,float seconds)
        {
            int count=(int)(22050*seconds);var samples=new float[count];for(int i=0;i<count;i++)samples[i]=Mathf.Sin(i*hz*Mathf.PI*2/22050)*(1-(float)i/count)*.2f;
            var clip=AudioClip.Create("Ember tone",count,1,22050,false);clip.SetData(samples,0);return clip;
        }
        bool Press(Key key)=>Keyboard.current!=null&&Keyboard.current[key].wasPressedThisFrame;
        bool Held(Key key)=>Keyboard.current!=null&&Keyboard.current[key].isPressed;
        void ReadInput()
        {
            if(Press(Key.Escape))
            {
                if(inventoryOpen)
                {
                    if(pendingQuickAssign>0)pendingQuickAssign=0;
                    else {inventoryOpen=false;dragFromCell=-1;dragFromQuick=-1;draggingItem=false;}
                }
                else if(craftOpen)craftOpen=false;
                else if(pauseConfirm||pauseSettings){pauseConfirm=false;pauseSettings=false;}
                else {menu=!menu;pauseConfirm=false;pauseSettings=false;if(menu)SaveActiveRun(false,asyncWrite:true);}
                path.Clear();
            }
            if(menu||debugOpen||State==null||State.Phase==Phase.Sanctuary||(network&&!host&&!string.IsNullOrEmpty(lan?.Error))){intent.X=intent.Y=0;return;}
            if(Local!=null&&Local.Ghost)
            {
                intent.X=intent.Y=0;inventoryOpen=false;craftOpen=false;path.Clear();
                if(Press(Key.A)||Press(Key.LeftArrow)||Press(Key.Q))CycleSpectate(-1);
                if(Press(Key.D)||Press(Key.RightArrow)||Press(Key.E)||Press(Key.Space))CycleSpectate(1);
                return;
            }
            if(Press(Key.I)){inventoryOpen=!inventoryOpen;if(!inventoryOpen){pendingQuickAssign=0;dragFromCell=-1;dragFromQuick=-1;draggingItem=false;}craftOpen=false;path.Clear();}
            if(Press(Key.Tab)){craftOpen=!craftOpen;inventoryOpen=false;pendingQuickAssign=0;}
            if(Press(Key.Digit1)||Press(Key.Numpad1)){if(inventoryOpen&&pendingQuickAssign>0)BindQuickSlot(0,pendingQuickAssign);else EquipSlot(quickSlots[0]);}
            if(Press(Key.Digit2)||Press(Key.Numpad2)){if(inventoryOpen&&pendingQuickAssign>0)BindQuickSlot(1,pendingQuickAssign);else EquipSlot(quickSlots[1]);}
            if(Press(Key.Digit3)||Press(Key.Numpad3)){if(inventoryOpen&&pendingQuickAssign>0)BindQuickSlot(2,pendingQuickAssign);else EquipSlot(quickSlots[2]);}
            if(Press(Key.Digit4)||Press(Key.Numpad4)){if(inventoryOpen&&pendingQuickAssign>0)BindQuickSlot(3,pendingQuickAssign);else EquipSlot(quickSlots[3]);}
            if(inventoryOpen){intent.X=intent.Y=0;return;}
            if(Press(Key.R))ranges=!ranges;
            if(Press(Key.M))
            {
                moba=!moba;
                PlayerPrefs.SetInt("Ember.Moba",moba?1:0);
                PlayerPrefs.Save();
                if(!moba)path.Clear();
                notice=moba?L("MOBA chuột phải: BẬT (click hoặc giữ chuột phải để di chuyển).","MOBA right-click: ON (click or hold RMB to move)."):L("MOBA chuột phải: TẮT.","MOBA right-click: OFF.");
            }
            Vector2 move=new Vector2((Held(Key.D)||Held(Key.RightArrow)?1:0)-(Held(Key.A)||Held(Key.LeftArrow)?1:0),(Held(Key.W)||Held(Key.UpArrow)?1:0)-(Held(Key.S)||Held(Key.DownArrow)?1:0));
            if(move.sqrMagnitude>0)path.Clear();
            bool overUI=IsMouseOverHud();
            if(moba&&Mouse.current!=null&&!overUI&&Local!=null)
            {
                bool rDown=Mouse.current.rightButton.wasPressedThisFrame;
                bool rHeld=Mouse.current.rightButton.isPressed;
                if(rDown||rHeld)
                {
                    Vector2 mp=Mouse.current.position.ReadValue();
                    Vector3 wp=cam.ScreenToWorldPoint(new Vector3(mp.x,mp.y,-cam.transform.position.z));
                    V2 target=new V2(wp.x,wp.y);
                    if(rDown)
                    {
                        MakePath(Local.Pos,target);
                        mobaHoldClock=clock+.15f;
                    }
                    else if(HasDirectWalkableLine(Local.Pos,target))
                    {
                        path.Clear();
                        path.Enqueue(target);
                        clickMarker=target;
                        clickMarkerUntil=clock+.25f;
                    }
                    else if(clock>=mobaHoldClock)
                    {
                        MakePath(Local.Pos,target);
                        mobaHoldClock=clock+.16f;
                    }
                }
            }
            if(move.sqrMagnitude==0&&path.Count>0&&Local!=null)
            {
                while(path.Count>1&&V2.Distance(path.Peek(),Local.Pos)<.42f)path.Dequeue();
                V2 delta=path.Peek()-Local.Pos;
                if(delta.Length<.16f)path.Dequeue();
                else move=new Vector2(delta.X,delta.Y).normalized;
            }
            intent.X=move.x;intent.Y=move.y;
            bool attack=(Held(Key.K)||(Mouse.current!=null&&Mouse.current.leftButton.isPressed&&!overUI))&&Local!=null&&IsHoldingWeaponSlot(heldSlot)&&Local.AttackCD<=0&&!Local.Reloading;intent.Attack|=attack;if(attack)audioSource.PlayOneShot(tone,.2f);
            bool pressedInteract=Press(Key.E);
            if(pressedInteract&&Local!=null&&!Local.Ghost&&ShouldAutoEquipPickaxe(Local))EquipSlot(8);
            intent.Skill|=Press(Key.J);intent.Dash|=Press(Key.Space);intent.Interact|=pressedInteract;intent.Refuel|=Press(Key.Q);intent.Deposit|=Press(Key.B);
            intent.ToggleLamp|=Press(Key.T);intent.PassLamp|=Press(Key.G);intent.Push|=Press(Key.L);intent.Revive|=Press(Key.H);intent.Reload|=Press(Key.X);
            if(Press(Key.U)&&CanUseSelectedItem(Local))intent.Use=selectedItem;
            ApplyTouchInput();
        }
        void ClearActions(){intent.Attack=intent.Skill=intent.Dash=intent.Interact=intent.Refuel=intent.Deposit=intent.ToggleLamp=intent.PassLamp=intent.Push=intent.Revive=intent.Reload=false;intent.Craft=intent.Use=-1;intent.Equip=0;}
        bool IsNavWalkable(byte[] tiles,int size,V2 pos)
        {
            int x=Mathf.FloorToInt(pos.X+size/2f),y=Mathf.FloorToInt(pos.Y+size/2f);
            if(x<3||y<3||x>=size-3||y>=size-3)return false;
            if(tiles[y*size+x]>=3)return false;
            if(V2.Distance(pos,new V2(0,.35f))<1.55f)return false;
            if(State!=null&&State.BossArenaActive&&V2.Distance(pos,State.BossArenaCenter)>State.BossArenaRadius-.45f)return false;
            if(host&&World!=null&&!World.Walkable(pos))return false;
            return true;
        }
        bool HasDirectWalkableLine(V2 from,V2 to)
        {
            if(State==null)return false;
            byte[] tiles=currentTiles??(host?World.Tiles:EmberWorld.MakeTiles(State.Seed,State.Size));
            int size=State.Size;
            float dist=V2.Distance(from,to);
            if(dist<.25f)return IsNavWalkable(tiles,size,to);
            if(dist>22f)return false;
            int steps=Mathf.CeilToInt(dist/.42f);
            for(int i=1;i<=steps;i++)
            {
                float t=(float)i/steps;
                V2 sample=new V2(Mathf.Lerp(from.X,to.X,t),Mathf.Lerp(from.Y,to.Y,t));
                if(!IsNavWalkable(tiles,size,sample))return false;
            }
            return true;
        }
        static readonly (int dx,int dy,int cost)[] EightDirs={
            (1,0,10),(-1,0,10),(0,1,10),(0,-1,10),
            (1,1,14),(1,-1,14),(-1,1,14),(-1,-1,14)
        };
        void MakePath(V2 from,V2 to)
        {
            path.Clear();
            if(State==null)return;
            int size=State.Size;
            byte[] tiles=currentTiles??(host?World.Tiles:EmberWorld.MakeTiles(State.Seed,size));
            if(State.BossArenaActive&&V2.Distance(to,State.BossArenaCenter)>State.BossArenaRadius-.5f)
            {
                V2 dir=(to-State.BossArenaCenter).Normal;
                to=State.BossArenaCenter+dir*(State.BossArenaRadius-.6f);
            }
            int sx=Mathf.Clamp(Mathf.FloorToInt(from.X+size/2f),3,size-4);
            int sy=Mathf.Clamp(Mathf.FloorToInt(from.Y+size/2f),3,size-4);
            int tx=Mathf.Clamp(Mathf.FloorToInt(to.X+size/2f),3,size-4);
            int ty=Mathf.Clamp(Mathf.FloorToInt(to.Y+size/2f),3,size-4);
            if(!IsNavWalkable(tiles,size,to))
            {
                int bestX=tx,bestY=ty;float bestDist=float.MaxValue;
                for(int r=1;r<=4;r++)for(int dy=-r;dy<=r;dy++)for(int dx=-r;dx<=r;dx++)
                {
                    int cx=Mathf.Clamp(tx+dx,3,size-4),cy=Mathf.Clamp(ty+dy,3,size-4);
                    V2 cand=new V2(cx-size/2f+.5f,cy-size/2f+.5f);
                    if(!IsNavWalkable(tiles,size,cand))continue;
                    float d=dx*dx+dy*dy;
                    if(d<bestDist){bestDist=d;bestX=cx;bestY=cy;}
                }
                if(bestDist<float.MaxValue){tx=bestX;ty=bestY;to=new V2(tx-size/2f+.5f,ty-size/2f+.5f);}
            }
            clickMarker=to;clickMarkerUntil=clock+.75f;
            if(HasDirectWalkableLine(from,to)){path.Enqueue(to);return;}
            if(sx==tx&&sy==ty){path.Enqueue(to);return;}

            // Fast zero-LINQ 8-directional A* using a compact binary min-heap.
            int start=sy*size+sx,target=ty*size+tx;
            var heapNode=new List<int>(256){start};
            var heapF=new List<int>(256){0};
            var gCost=new Dictionary<int,int>(256){{start,0}};
            var parent=new Dictionary<int,int>(256);
            int limit=1800;
            while(heapNode.Count>0&&limit-->0)
            {
                int current=heapNode[0];
                int last=heapNode.Count-1;
                heapNode[0]=heapNode[last];heapF[0]=heapF[last];
                heapNode.RemoveAt(last);heapF.RemoveAt(last);
                int idx=0;
                while(true)
                {
                    int l=idx*2+1,r=l+1,sm=idx;
                    if(l<heapNode.Count&&heapF[l]<heapF[sm])sm=l;
                    if(r<heapNode.Count&&heapF[r]<heapF[sm])sm=r;
                    if(sm==idx)break;
                    (heapNode[idx],heapNode[sm])=(heapNode[sm],heapNode[idx]);
                    (heapF[idx],heapF[sm])=(heapF[sm],heapF[idx]);
                    idx=sm;
                }
                if(current==target)break;
                int x=current%size,y=current/size,baseG=gCost[current];
                for(int d=0;d<EightDirs.Length;d++)
                {
                    var dir=EightDirs[d];
                    int nx=x+dir.dx,ny=y+dir.dy;
                    if(nx<3||ny<3||nx>=size-3||ny>=size-3)continue;
                    if(dir.dx!=0&&dir.dy!=0)
                    {
                        if(tiles[y*size+nx]>=3||tiles[ny*size+x]>=3)continue;
                    }
                    V2 cellPos=new V2(nx-size/2f+.5f,ny-size/2f+.5f);
                    if(!IsNavWalkable(tiles,size,cellPos))continue;
                    int next=ny*size+nx,nextG=baseG+dir.cost;
                    if(gCost.TryGetValue(next,out int oldG)&&oldG<=nextG)continue;
                    gCost[next]=nextG;parent[next]=current;
                    int adx=Math.Abs(nx-tx),ady=Math.Abs(ny-ty);
                    int h=10*(adx+ady)-6*Math.Min(adx,ady);
                    int f=nextG+h;
                    heapNode.Add(next);heapF.Add(f);
                    int cur=heapNode.Count-1;
                    while(cur>0)
                    {
                        int par=(cur-1)>>1;
                        if(heapF[par]<=heapF[cur])break;
                        (heapNode[par],heapNode[cur])=(heapNode[cur],heapNode[par]);
                        (heapF[par],heapF[cur])=(heapF[cur],heapF[par]);
                        cur=par;
                    }
                }
            }
            if(!parent.ContainsKey(target)){path.Enqueue(to);return;}
            var raw=new List<V2>(64);
            int at=target;
            while(at!=start){raw.Add(new V2(at%size-size/2f+.5f,at/size-size/2f+.5f));at=parent[at];}
            raw.Reverse();
            if(raw.Count>0)raw[raw.Count-1]=to;
            // Line-of-sight string pulling removes stair-step jitter so MOBA paths glide smoothly.
            V2 anchor=from;
            int cursor=0;
            while(cursor<raw.Count)
            {
                int furthest=cursor;
                int maxLook=Math.Min(raw.Count-1,cursor+12);
                for(int look=maxLook;look>cursor;look--)
                {
                    if(HasDirectWalkableLine(anchor,raw[look])){furthest=look;break;}
                }
                path.Enqueue(raw[furthest]);
                anchor=raw[furthest];
                cursor=furthest+1;
            }
        }
        Sprite Primitive(string name)
        {
            if(primitives.TryGetValue(name,out var cached))return cached;
            var authored=Resources.Load<Texture2D>("Ember/Props/"+name);
            if(authored!=null){authored.filterMode=FilterMode.Point;bool rooted=name.StartsWith("enemy_")||name=="nest"||name=="boss"||name=="beacon";cached=Sprite.Create(authored,new Rect(0,0,authored.width,authored.height),new Vector2(.5f,rooted?.05f:.5f),32);primitives[name]=cached;return cached;}
            int size=16;Texture2D t=new Texture2D(size,size,TextureFormat.RGBA32,false);t.filterMode=FilterMode.Point;var c=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float r=Vector2.Distance(new Vector2(x,y),new Vector2(7.5f,7.5f));bool draw=false;Color color=Color.white;
                switch(name)
                {
                    case "circle":draw=r<7.5f;break;
                    case "ring":draw=r<7.5f&&r>5.8f;break;
                    case "diamond":draw=Math.Abs(x-8)+Math.Abs(y-8)<7;color=x<8?new Color(.7f,1,1):new Color(.35f,.7f,.8f);break;
                    case "tree":draw=y<5?(x>6&&x<10):Math.Abs(x-8)<(16-y)*.65f;color=y<5?new Color(.2f,.15f,.2f):x<8?new Color(.16f,.32f,.29f):new Color(.1f,.22f,.23f);break;
                    case "enemy":draw=(x>2&&x<13&&y>2&&y<12)||(y>=12&&y<15&&(x==3||x==11));color=y==8&&(x==5||x==10)?Coral:new Color(.25f,.2f,.35f);break;
                    case "hand":draw=x>=4&&x<=11&&y>=5&&y<=11;color=x==4||x==11||y==5||y==11?new Color(.12f,.13f,.2f):Color.white;break;
                    case "stick":draw=x>=7&&x<=9&&y>=2&&y<=14;color=x==7?new Color(.35f,.19f,.12f):new Color(.75f,.48f,.24f);break;
                    case "beacon":draw=y<3?x>2&&x<14:y<10?x>5&&x<11:r<5;color=y<3?new Color(.25f,.29f,.39f):y<10?new Color(.5f,.54f,.6f):Cyan;break;
                    default:draw=x>=2&&x<14&&y>=2&&y<14;break;
                }
                if(draw)c[y*size+x]=color;
            }
            t.SetPixels32(c);t.Apply();cached=Sprite.Create(t,new Rect(0,0,size,size),new Vector2(.5f,.5f),16);primitives[name]=cached;return cached;
        }
        Sprite Frame(Vessel vessel,string clip,int frame,bool eyes=false)
        {
            if(vessel==Vessel.LittleMoone)return eyes?null:MooneFrame(clip,frame);
            string slug=Art[(int)vessel]+(eyes?"_eyes":"");if(!atlases.TryGetValue(slug,out var sprites))
            {
                Texture2D tex=Resources.Load<Texture2D>("Ember/Active/"+slug);if(tex==null)return eyes?null:Primitive("enemy");tex.filterMode=FilterMode.Point;int columns=tex.width/32;sprites=new Sprite[columns*4];
                for(int row=0;row<4;row++)for(int f=0;f<columns;f++)sprites[row*columns+f]=Sprite.Create(tex,new Rect(f*32,tex.height-(row+1)*32,32,32),new Vector2(.5f,1/32f),32);
                atlases[slug]=sprites;
            }
            int s=clip=="run"?1:clip=="cast"?2:clip=="hurt"?3:0,count=sprites.Length/4;return sprites[s*count+frame%count];
        }
        static readonly string[][] MooneFrames={new[]{"idle right 0 [cell_0_0]","idle right 1 [cell_0_1]","idle right 2 [cell_0_2]","idle right 3 [cell_0_3]","idle right 4 [cell_0_4]","idle right 5 [cell_0_5]","idle right 6 [cell_0_6]","idle right 7 [cell_0_7]"},new[]{"walk_right - Frame 0 [cell_1_0]","walk_right - Frame 1 [cell_1_1]","walk_right - Frame 2 [cell_1_2]","walk_right - Frame 3 [cell_1_3]","walk_right - Frame 4 [cell_1_4]","walk_right - Frame 5 [cell_1_5]","walk_right - Frame 6 [cell_1_6]","walk_right - Frame 7 [cell_1_7]"},new[]{"cast_right - Frame 0 [cast_right_0]","cast_right - Frame 1 [cast_right_1]","cast_right - Frame 2 [cast_right_2]","cast_right - Frame 3 [cast_right_3]","cast_right - Frame 4 [cast_right_4]","cast_right - Frame 5 [cast_right_5]","cast_right - Frame 6 [cast_right_6]","cast_right - Frame 7 [cast_right_7]"},new[]{"hurt_right - Frame 0 [hurt_right_0]","hurt_right - Frame 1 [hurt_right_1]","hurt_right - Frame 2 [hurt_right_2]","hurt_right - Frame 3 [hurt_right_3]","hurt_right - Frame 4 [hurt_right_4]","hurt_right - Frame 5 [hurt_right_5]","hurt_right - Frame 6 [hurt_right_6]","hurt_right - Frame 7 [hurt_right_7]"}};
        Dictionary<string,Sprite> mooneSprites;
        Sprite MooneFrame(string clip,int frame)
        {
            if(mooneSprites==null)mooneSprites=Resources.LoadAll<Sprite>("Characters/Little Moone/silver_wanderer___approved_concept_reconstruction")
                .ToDictionary(sprite=>sprite.name);
            int row=clip=="run"?1:clip=="cast"?2:clip=="hurt"?3:0;
            return mooneSprites.TryGetValue(MooneFrames[row][frame%8],out var sprite)?sprite:Primitive("enemy");
        }
        readonly Dictionary<string,Sprite[]> enemySprites=new Dictionary<string,Sprite[]>();
        readonly Dictionary<int,bool> enemyFacingLeft=new Dictionary<int,bool>();
        Sprite EnemyFrame(string art,int frame)
        {
            if(!enemySprites.TryGetValue(art,out var frames))
            {
                var tex=Resources.Load<Texture2D>("Ember/Props/"+art);
                if(tex==null)return Primitive(art);
                tex.filterMode=FilterMode.Point;
                int frameH=tex.height;
                int frameW=frameH;
                int count=Mathf.Max(1,tex.width/frameW);
                frames=new Sprite[count];
                for(int i=0;i<count;i++)
                    frames[i]=Sprite.Create(tex,new Rect(i*frameW,0,frameW,frameH),new Vector2(.5f,.05f),32);
                enemySprites[art]=frames;
            }
            return frames[Mathf.Abs(frame)%frames.Length];
        }
        Sprite Glove(bool grip)
        {
            string key=grip?"white_glove_grip":"white_glove_relaxed";if(primitives.TryGetValue(key,out var cached))return cached;
            var texture=Resources.Load<Texture2D>("Ember/Active/white_gloves_cupped");if(texture==null)return Primitive("hand");texture.filterMode=FilterMode.Point;cached=Sprite.Create(texture,new Rect(0,grip?0:16,16,16),new Vector2(.5f,.5f),32);primitives[key]=cached;return cached;
        }
        SpriteRenderer Put(string key,Sprite sprite,V2 pos,Color color,float scale=1,int? order=null,bool flip=false)
        {
            touched.Add(key);if(!renderers.TryGetValue(key,out var r)){r=new GameObject(key).AddComponent<SpriteRenderer>();r.transform.SetParent(entityRoot.transform);if(spriteMaterial!=null)r.sharedMaterial=spriteMaterial;renderers[key]=r;}
            r.enabled=true;r.sprite=sprite;r.color=color;r.flipX=flip;r.transform.position=new Vector3(pos.X,pos.Y,0);r.transform.localScale=Vector3.one*scale;r.sortingOrder=order??(1000-Mathf.RoundToInt(pos.Y*10));return r;
        }
        void Render()
        {
            touched.Clear();var local=Local;var focus=CameraTarget();V2 center=focus!=null?VisualPlayerPos(focus):new V2();
            int pixels=CameraZoomPixels[zoomLevel];
            cam.orthographicSize=Screen.height/(2f*pixels);
            cam.transform.position=new Vector3(center.X,center.Y,-10);
            float viewHalfH=cam.orthographicSize+2.4f;
            float viewHalfW=cam.orthographicSize*cam.aspect+2.4f;
            if(Mathf.Abs(center.X)<=viewHalfW+3f&&Mathf.Abs(center.Y)<=viewHalfH+3f)
                Put("beacon",Primitive("beacon"),new V2(),State.BeaconHP>0&&State.Charge>=State.ChargeTarget?Color.white:Color.gray,3);
            if(local!=null&&local.MirageLife>0)Put("mirage",Primitive("box"),local.MiragePos,Cyan*(.8f+.2f*Mathf.Sin(clock*3)),.7f);
            if(State.Phase==Phase.Extraction&&Mathf.Abs(center.X)<=viewHalfW+4f&&Mathf.Abs(center.Y)<=viewHalfH+4f)
                Put("portal",Primitive("ring"),new V2(),Cyan*(.8f+.2f*Mathf.Sin(clock*4)),4,-20);
            var nodesArr=State.Nodes;
            V2 localVisual=local!=null?VisualPlayerPos(local):center;
            for(int ni=0;ni<nodesArr.Length;ni++)
            {
                var n=nodesArr[ni];
                if(Mathf.Abs(n.Pos.X-center.X)>viewHalfW||Mathf.Abs(n.Pos.Y-center.Y)>viewHalfH)continue;
                bool tree=!n.Dropped&&(n.Resource==Resource.Wood||n.Resource==Resource.Dust);
                Color color=n.Resource==Resource.Core?Cyan:n.Resource==Resource.Umbra?new Color(.7f,.4f,1):n.Resource==Resource.Dust?Gold:n.Resource==Resource.Wood?new Color(.5f,.35f,.2f):new Color(.6f,.65f,.8f);
                Sprite sprite=tree?Primitive("tree"):MaterialSprite(ResourceIcons[(int)n.Resource]);
                float scale=tree?1.5f:n.Dropped?.70f:.62f;
                V2 drawPos=n.Dropped?n.Pos+new V2(0,Mathf.Sin(clock*4.5f+n.Id)*.07f+.08f):n.Pos;
                int order=1000-Mathf.RoundToInt(n.Pos.Y*10);
                bool showGoldGlow=n.Dropped||!tree||(local!=null&&!local.Ghost&&V2.Distance(n.Pos,localVisual)<2.4f);
                if(showGoldGlow)
                {
                    float pulse=.82f+.18f*Mathf.Sin(clock*6f+n.Id);
                    Color glowGold=new Color(1f,.84f,.24f,pulse);
                    Outline(Put("nodeGlow"+n.Id,sprite,drawPos,glowGold,scale,order-1),sprite);
                }
                Put("node"+n.Id,sprite,drawPos,tree?(n.Resource==Resource.Dust?new Color(.6f,1,.8f):color):Color.white,scale,order);
            }
            var structsArr=State.Structures;
            for(int si=0;si<structsArr.Length;si++)
            {
                var s=structsArr[si];
                if(Mathf.Abs(s.Pos.X-center.X)>viewHalfW||Mathf.Abs(s.Pos.Y-center.Y)>viewHalfH)continue;
                Put("structure"+s.Id,Primitive(s.Kind<2?"torch":s.Kind==2?"wall":s.Kind==3?"trap":s.Kind==4?"chest":"diamond"),s.Pos,Color.white,s.Kind==2?1.4f:1);
            }
            var enemiesArr=State.Enemies;
            for(int ei=0;ei<enemiesArr.Length;ei++)
            {
                var e=enemiesArr[ei];
                V2 ePos=VisualEnemyPos(e);
                if(Mathf.Abs(ePos.X-center.X)>viewHalfW+1.5f||Mathf.Abs(ePos.Y-center.Y)>viewHalfH+1.5f)continue;
                string art=e.Kind==EnemyKind.Boss?"boss":e.Kind==EnemyKind.Nest?"nest":e.Kind==EnemyKind.SwampLeech?"enemy_swamp":e.Kind==EnemyKind.VoidStalker?"enemy_void":e.Elite?"enemy_sentinel":"enemy_shade";
                float scale=e.IsWorldBoss?Catalog.BossFor(e.BossType).Scale:e.Kind==EnemyKind.Boss?1.5f:e.Elite?1.4f:1;
                Color glowColor=e.IsWorldBoss?BossAccentColor(e.BossType):Coral;
                Color bodyTint=e.Blind>0?new Color(.6f,.7f,.8f):e.IsWorldBoss?BossTintColor(e.BossType):Color.white;
                bool moving=false;
                if(prevEnemyPos.TryGetValue(e.Id,out var prevEPos))
                {
                    moving=V2.Distance(prevEPos,e.Pos)>0.02f;
                    if(Mathf.Abs(e.Pos.X-prevEPos.X)>0.012f)enemyFacingLeft[e.Id]=e.Pos.X<prevEPos.X;
                }
                enemyFacingLeft.TryGetValue(e.Id,out bool flip);
                float animFps=e.Kind==EnemyKind.Nest?0f:moving?7.5f:3.2f;
                int animFrame=animFps<=0?0:Mathf.FloorToInt(clock*animFps+(e.Id*1.37f))%4;
                var artSprite=EnemyFrame(art,animFrame);
                float bob=e.Kind==EnemyKind.Nest?0f:moving?Mathf.Sin(clock*13f+e.Id)*.035f:Mathf.Sin(clock*3.2f+e.Id)*.018f;
                V2 renderPos=ePos+new V2(0,bob);
                Outline(Put("enemyGlow"+e.Id,artSprite,renderPos,glowColor,scale,1000-Mathf.RoundToInt(ePos.Y*10)-1,flip),artSprite);
                Put("enemy"+e.Id,artSprite,renderPos,bodyTint,scale,flip:flip);
            }
            var playersArr=State.Players;
            for(int pi=0;pi<playersArr.Length;pi++)
            {
                var p=playersArr[pi];
                if(p.Extracted||p.Ghost)continue;
                V2 pPos=VisualPlayerPos(p);
                bool hallucination=local!=null&&!local.Ghost&&p.Id!=local.Id&&local.Mad;Color glow=hallucination?Coral:Cyan;
                string clip=p.CastTime>0?"cast":p.Invulnerable>0&&p.DashTime<=0?"hurt":p.Move.Length>.1f?"run":"idle";
                if(!animationState.TryGetValue(p.Id,out var anim)||anim.clip!=clip){anim=(clip,clock);animationState[p.Id]=anim;}
                int frames=p.Vessel==Vessel.LittleMoone||Art[(int)p.Vessel]=="lyra_moonscout"?8:4;
                int raw=(int)((clock-anim.began)*(p.Vessel==Vessel.LittleMoone?6:clip=="run"?10:clip=="cast"?(frames==8?12:9):clip=="hurt"?(frames==8?10:8):6));int f=clip=="cast"||clip=="hurt"?Mathf.Min(frames-1,raw):raw%frames;
                var sprite=hallucination?Primitive("enemy_void"):Frame(p.Vessel,clip,f);Color tint=Color.white;
                Put("shadow"+p.Id,Primitive("circle"),pPos,new Color(0,0,0,.35f),.8f,-10);
                Outline(Put("outline"+p.Id,sprite,pPos,glow,1,1000-Mathf.RoundToInt(pPos.Y*10)-1,!p.Right),sprite);
                var body=Put("player"+p.Id,sprite,pPos,tint,1,flip:!p.Right);
                if(!hallucination){var eyes=Frame(p.Vessel,clip,f,true);if(eyes!=null){var glowEyes=Put("eyes"+p.Id,eyes,pPos,tint,1,1001-Mathf.RoundToInt(pPos.Y*10),!p.Right);if(emissionMaterial!=null)glowEyes.sharedMaterial=emissionMaterial;}}
                if(!hallucination&&p.CastTime<=0)DrawHands(p,body,pPos);
                if(p.Hat>=0&&!hallucination)Put("hat"+p.Id,Primitive(new[]{"hat_straw","hat_witch","hat_candle","hat_jester"}[p.Hat]),pPos+new V2(0,p.Hat==3?.47f:.82f),Color.white,p.Hat==3?.7f:.8f,1010-Mathf.RoundToInt(pPos.Y*10));
                if(p.Pet>=0&&!hallucination)Put("pet"+p.Id,Primitive(new[]{"pet_firefly","pet_bat","pet_cat","pet_ghost"}[p.Pet]),pPos+new V2(-1,.1f+Mathf.Sin(clock*4)*.15f),Color.white,.65f);
                if(p.ShieldTime>0)Put("shield"+p.Id,Primitive("ring"),pPos+new V2(0,.5f),Cyan,2,-1);
            }
            var projsArr=State.Projectiles;
            if(projsArr!=null)for(int qi=0;qi<projsArr.Length;qi++)
            {
                var shot=projsArr[qi];
                V2 sPos=VisualProjectilePos(shot);
                if(Mathf.Abs(sPos.X-center.X)>viewHalfW+1f||Mathf.Abs(sPos.Y-center.Y)>viewHalfH+1f)continue;
                if(shot.FromEnemy)
                {
                    Color orbColor=EnemyOrbColor(shot.ColorKind);
                    float baseScale=shot.ColorKind>=4?.33f:.42f;
                    float pulse=baseScale*(1f+.09f*Mathf.Sin(clock*9f+shot.Id));
                    var r=Put("projectile"+shot.Id,Primitive("circle"),sPos+new V2(0,.3f),orbColor,pulse,1500);
                    r.transform.rotation=Quaternion.identity;
                    if(emissionMaterial!=null)r.sharedMaterial=emissionMaterial;
                    continue;
                }
                var pr=Put("projectile"+shot.Id,Primitive("diamond"),sPos+new V2(0,.3f),shot.Weapon==Weapon.ShadowBlades?new Color(.7f,.4f,1):new Color(.78f,.98f,1f),.30f);pr.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(shot.Velocity.Y,shot.Velocity.X)*Mathf.Rad2Deg);if(emissionMaterial!=null)pr.sharedMaterial=emissionMaterial;
            }
            if(local!=null&&local.Dread>=30&&!local.Safe&&Mathf.Sin(clock*.8f)>0.6f)
            {
                float sign=Mathf.Sin(clock*.3f)>0?1:-1;var pos=localVisual+new V2(sign*(cam.orthographicSize*cam.aspect-.6f),Mathf.Sin(clock*1.2f)*3);
                Put("falseShadow",Primitive("enemy_void"),pos,new Color(.08f,.06f,.16f,.65f),1.3f,2000);
            }
            foreach(var pair in renderers)if(!touched.Contains(pair.Key)&&pair.Value.enabled)pair.Value.enabled=false;
            // Reclaim transient node/enemy renderers instead of retaining an entire run.
            if(renderers.Count>1400)foreach(var key in renderers.Where(p=>!p.Value.enabled).Select(p=>p.Key).ToArray()){Destroy(renderers[key].gameObject);renderers.Remove(key);}
        }
        void BuildMap()
        {
            renderedSeed=State.Seed;foreach(Transform t in mapRoot.transform)Destroy(t.gameObject);
            visibleTrees.Clear();treePool.Clear();visibleChunk=new Vector2Int(int.MinValue,int.MinValue);
            float edgeWorld=State.Size/2f-3;
            var fog=new GameObject("Abyss / unlit impenetrable fog").AddComponent<SpriteRenderer>();fog.transform.SetParent(mapRoot.transform);fog.sprite=Sprite.Create(white,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);fog.transform.localScale=Vector3.one*(State.Size+40);fog.sortingOrder=30000;
            var fogMaterial=Resources.Load<Material>("Ember/EmberAbyss");if(fogMaterial!=null){fog.material=new Material(fogMaterial);fog.material.SetFloat("_Boundary",edgeWorld);}
            foreach(var side in new[]{new Vector3(-edgeWorld-2,0),new Vector3(edgeWorld+2,0),new Vector3(0,-edgeWorld-2),new Vector3(0,edgeWorld+2)})
            {
                var border=new GameObject("Abyss / physical boundary");border.transform.SetParent(mapRoot.transform);border.transform.position=side;
                var collider=border.AddComponent<BoxCollider2D>();collider.size=side.x!=0?new Vector2(4,State.Size+8):new Vector2(State.Size+8,4);
            }
            byte[] tiles=host?World.Tiles:EmberWorld.MakeTiles(State.Seed,State.Size);currentTiles=tiles;int size=State.Size;
            // One texture for all floor tiles; point sampling preserves 8px terrain.
            int px=size*4;Texture2D texture=new Texture2D(px,px,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Point;var colors=new Color32[px*px];var random=new System.Random(State.Seed);
            Color[] bases={new Color(.3f,.42f,.36f),new Color(.26f,.36f,.4f),new Color(.34f,.25f,.42f),new Color(.12f,.19f,.19f),Color.black};
            for(int y=0;y<px;y++)for(int x=0;x<px;x++)
            {
                byte tile=tiles[(y/4)*size+x/4];Color c=bases[tile];float noise=tile==4?0:(float)random.NextDouble()*.045f;bool edge=x%4==0||y%4==0;colors[y*px+x]=c*(edge?.83f:1)+new Color(noise,noise,noise,0);
            }
            texture.SetPixels32(colors);texture.Apply();var floor=new GameObject("Three biomes").AddComponent<SpriteRenderer>();floor.transform.SetParent(mapRoot.transform);if(spriteMaterial!=null)floor.sharedMaterial=spriteMaterial;floor.sprite=Sprite.Create(texture,new Rect(0,0,px,px),new Vector2(.5f,.5f),4);floor.sortingOrder=-100;
        }
        readonly HashSet<int> keepTrees=new HashSet<int>();
        readonly List<int> dropTrees=new List<int>();
        void StreamTrees()
        {
            if(currentTiles==null||State==null)return;var pos=CameraTarget()?.Pos??new V2();int size=State.Size;
            var chunk=new Vector2Int(Mathf.FloorToInt(pos.X/4),Mathf.FloorToInt(pos.Y/4));if(chunk==visibleChunk)return;visibleChunk=chunk;
            int halfX=Mathf.CeilToInt(cam.orthographicSize*cam.aspect)+6;
            int halfY=Mathf.CeilToInt(cam.orthographicSize)+6;
            int cx=chunk.x*4+size/2,cy=chunk.y*4+size/2;
            keepTrees.Clear();dropTrees.Clear();
            var treeSprite=Primitive("tree");
            int yMin=Mathf.Max(3,cy-halfY),yMax=Mathf.Min(size-3,cy+halfY+2);
            int xMin=Mathf.Max(3,cx-halfX),xMax=Mathf.Min(size-3,cx+halfX+2);
            for(int y=yMin;y<yMax;y++)for(int x=xMin;x<xMax;x++)if(currentTiles[y*size+x]==3)
            {
                int key=y*size+x;keepTrees.Add(key);if(visibleTrees.ContainsKey(key))continue;
                SpriteRenderer tree;
                if(treePool.Count>0){tree=treePool.Pop();tree.enabled=true;}
                else
                {
                    tree=new GameObject("Ancient tree").AddComponent<SpriteRenderer>();
                    tree.transform.SetParent(mapRoot.transform);
                    if(spriteMaterial!=null)tree.sharedMaterial=spriteMaterial;
                    tree.sprite=treeSprite;
                    tree.transform.localScale=new Vector3(1.5f,2,1);
                }
                tree.transform.position=new Vector3(x-size/2+.5f,y-size/2+.75f,0);
                tree.sortingOrder=1000-Mathf.RoundToInt((y-size/2+.5f)*10);
                visibleTrees[key]=tree;
            }
            foreach(var kv in visibleTrees)if(!keepTrees.Contains(kv.Key))dropTrees.Add(kv.Key);
            for(int i=0;i<dropTrees.Count;i++)
            {
                int key=dropTrees[i];
                var sr=visibleTrees[key];
                sr.enabled=false;
                treePool.Push(sr);
                visibleTrees.Remove(key);
            }
        }
        void InitStyles()
        {
            if(title!=null)return;title=new GUIStyle(GUI.skin.label){fontSize=27,fontStyle=FontStyle.Bold,normal={textColor=new Color(1,.92f,.78f)}};
            label=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true,normal={textColor=new Color(.88f,.91f,.94f)}};
            small=new GUIStyle(label){fontSize=12};GUI.skin.button.fontSize=14;GUI.skin.button.padding=new RectOffset(12,12,8,8);
        }
        void Panel(Rect rect,Color color){GUI.color=color;GUI.DrawTexture(rect,white);GUI.color=Color.white;}
        Vector2 ScreenPos(V2 p){Vector3 v=cam.WorldToScreenPoint(new Vector3(p.X,p.Y));return new Vector2(v.x,Screen.height-v.y);}
        static readonly Vector2[] RingPoints=CreateRingPoints();

        static Vector2[] CreateRingPoints()
        {
            var points=new Vector2[64];
            for(int i=0;i<points.Length;i++)
            {
                float angle=i*Mathf.PI*2/points.Length;
                points[i]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            }
            return points;
        }

        void Ring(V2 pos,float radius,Color color,float width=1.6f)
        {
            if(Event.current.type!=EventType.Repaint||radius<=0)return;
            Vector2 center=ScreenPos(pos);
            float scale=radius*Screen.height/(cam.orthographicSize*2);
            GUI.color=color;
            Vector2 previous=center+RingPoints[RingPoints.Length-1]*scale;
            foreach(var point in RingPoints)
            {
                Vector2 next=center+point*scale;
                Line(previous,next,width);
                previous=next;
            }
            GUI.color=Color.white;
        }

        void GlowRing(V2 pos,float radius,Color color,float width=2f)
        {
            if(Event.current.type!=EventType.Repaint||radius<=0)return;
            Ring(pos,radius,new Color(color.r,color.g,color.b,color.a*.32f),width*2.6f);
            Ring(pos,radius,color,width);
        }

        void DrawRangeRulers(Player p)
        {
            if(Event.current.type!=EventType.Repaint||p.Ghost)return;
            V2 pVis=VisualPlayerPos(p);
            if(ranges&&IsHoldingWeaponSlot(heldSlot))
            {
                Weapon activeWeapon=WeaponForSlot(p,heldSlot);
                float weaponRange=WeaponRangeFor(activeWeapon);
                bool hasNode=heldSlot==8&&State.Nodes.Any(n=>V2.Distance(n.Pos,p.Pos)<weaponRange);
                bool basic=State.Enemies.Any(e=>e.HP>0&&V2.Distance(e.Pos,p.Pos)<weaponRange)||
                    (p.Mad&&State.Players.Any(q=>q.Id!=p.Id&&!q.Ghost&&!q.Extracted&&!q.Safe&&V2.Distance(q.Pos,p.Pos)<weaponRange));
                Color weaponColor=hasNode?new Color(.52f,1f,.64f,.95f):basic?new Color(1f,.66f,.26f,.95f):new Color(.62f,.92f,1f,.54f);
                GlowRing(pVis,weaponRange,weaponColor,basic||hasNode?2.4f:1.8f);
            }

            if(p.Vessel!=Vessel.Wanderer&&p.SkillRange>0)
            {
                bool hasTarget=HasSkillFocus(p);
                bool ready=p.SkillCD<=0;
                if(hasTarget&&ready)
                {
                    float blink=.5f+.5f*Mathf.Sin(clock*14f);
                    Color activeColor=Color.Lerp(new Color(1f,.20f,.22f,.35f),new Color(1f,.58f,.58f,1f),blink);
                    GlowRing(pVis,p.SkillRange,activeColor,2.2f+1.4f*blink);
                }
                else if(hasTarget)
                {
                    float slow=.5f+.5f*Mathf.Sin(clock*5f);
                    GlowRing(pVis,p.SkillRange,new Color(1f,.22f,.24f,.38f+.20f*slow),1.9f);
                }
                else
                {
                    Ring(pVis,p.SkillRange,new Color(.95f,.20f,.24f,.45f),1.8f);
                }
            }
        }

        void Line(Vector2 a,Vector2 b,float width)
        {
            if(Event.current.type!=EventType.Repaint)return;
            if(Mathf.Max(a.x,b.x)<-width||Mathf.Min(a.x,b.x)>Screen.width+width||
               Mathf.Max(a.y,b.y)<-width||Mathf.Min(a.y,b.y)>Screen.height+width)return;
            var matrix=GUI.matrix;
            Vector2 delta=b-a;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,a);
            GUI.DrawTexture(new Rect(a.x,a.y,delta.magnitude,width),white);
            GUI.matrix=matrix;
        }
        void DrawInterface()
        {
            InitStyles();
            if(network&&!host&&!string.IsNullOrEmpty(lan?.Error)){DrawConnectionIssue(lan.Error);return;}
            if(network&&(State==null||State.Phase==Phase.Sanctuary)){DrawLobby();return;}
            if(menu)
            {
                if(pendingRestore!=null)DrawRestoreDialog();
                else if(State!=null&&Local!=null&&State.Phase!=Phase.Sanctuary)
                {
                    if(State.Phase==Phase.Won||State.Phase==Phase.Lost)DrawRunResult(Local,false);
                    else DrawPauseMenu();
                }
                else DrawMenu();
                return;
            }
            if(State==null){UiLabel(new Rect(30,30,700,80),"Đang chờ host... ESC để quay lại.\n"+notice,label);return;}
            var p=Local;if(p==null)return;
            bool awaitingTeam=p.Extracted&&State.Phase==Phase.Extraction;
            if(awaitingTeam||State.Phase==Phase.Won||State.Phase==Phase.Lost)
            {
                DrawRunResult(p,awaitingTeam);
                return;
            }
            if(p.Dread>=60&&!p.Safe&&Event.current.type==EventType.Repaint)
                Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.8f,.2f,.4f,.2f));
            if(clock<clickMarkerUntil)Ring(clickMarker,.3f,new Color(1,.8f,.4f,(clickMarkerUntil-clock)/.8f));
            DrawBossArena();
            DrawBossTelegraphs();
            DrawRangeRulers(p);
            foreach(var e in State.Effects){
                if(e.Kind==6){Ring(e.To,.35f+(.3f-e.Life)*2,new Color(.83f,.73f,1,e.Life/.3f));continue;}
                if(e.Kind==7){DrawHarvestChips(e);continue;}
                if(e.Kind==4||e.Kind==5){DrawMeteorEffect(e);continue;}
                GUI.color=e.Kind==0?Gold:e.Kind==3?Coral:Cyan;Line(ScreenPos(e.From),ScreenPos(e.To),3);if(e.Kind>=2)Ring(e.To,1+( .3f-e.Life)*6,GUI.color);GUI.color=Color.white;}
            DrawStructureBars();
            DrawEnemyHealthBars();
            DrawCombatPopups();
            foreach(var q in State.Players)
            {
                if(q.Extracted||q.Ghost||(!p.Ghost&&p.Mad&&q.Id!=p.Id))continue;
                DrawOverheadStatus(q);
            }
            if(!p.Ghost&&!inventoryOpen&&!craftOpen)DrawInteractPrompt(p);
            if(!p.Ghost&&inventoryOpen){DrawInventory(p);return;}
            if(Event.current.type==EventType.Repaint){hudBlockingRects.Clear();hudCapturing=true;}
            DrawFieldHud(p);
            DrawBossHud();
            float objectiveY=ObjectiveCardY;
            DrawObjectiveCard(p,objectiveY);
            DrawFieldNotices(p,objectiveY+80);
            if(!p.Ghost&&craftOpen)DrawCraft(p);
            hudCapturing=false;

        }
        void DrawDarkness()
        {
            if(Local==null)return;
            // Coarse screen-space light mask: alpha varies per cell; light sources
            // are authoritative positions, independently of hallucinated sprite colors.
            const int cell=32;for(int y=92;y<Screen.height;y+=cell)for(int x=0;x<Screen.width;x+=cell)
            {
                Vector3 point=cam.ScreenToWorldPoint(new Vector3(x+cell/2,Screen.height-y-cell/2));V2 world=new V2(point.x,point.y);float light=0;
                if(State.BeaconHP>0)light=Mathf.Max(light,1-world.Length/7);
                if(State.Fuel>0||State.LastLight>0)foreach(var p in State.Players.Where(q=>q.CarriesLantern&&q.LampOn&&!q.Ghost&&!q.Extracted))light=Mathf.Max(light,1-V2.Distance(world,p.Pos)/(p.Lantern==Lantern.Relic?9:6));
                foreach(var p in State.Players.Where(q=>!q.Ghost&&!q.Extracted&&q.Hat==(int)Hat.Candle))light=Mathf.Max(light,1-V2.Distance(world,p.Pos)/2);
                foreach(var s in State.Structures.Where(q=>q.Kind<=1))light=Mathf.Max(light,1-V2.Distance(world,s.Pos)/(s.Kind==1?9:5));
                float border=State.Size/2-3-Mathf.Max(Mathf.Abs(world.X),Mathf.Abs(world.Y));float alpha=Mathf.Lerp(.87f,.08f,Mathf.Clamp01(light*1.5f));if(border<5)alpha=Mathf.Max(alpha,1-Mathf.Clamp01(border/5));
                Panel(new Rect(x,y,cell,cell),new Color(.01f,.015f,.04f,alpha));
            }
        }
        void DrawCraft(Player p)
        {
            float width=Mathf.Min(360,Screen.width-28);
            Rect panel=new Rect(Screen.width-width-14,100,width,Mathf.Max(108,Screen.height-224));
            PixelPanel(panel,FieldPaper);
            bool prevCapture=hudCapturing;hudCapturing=false;
            GUILayout.BeginArea(new Rect(panel.x+12,panel.y+10,panel.width-24,panel.height-20));
            UiText(L("Chế tạo","Crafting"),title);
            scroll=GUILayout.BeginScrollView(scroll);
            for(int i=0;i<Catalog.Recipes.Length;i++)
            {
                var recipe=Catalog.Recipes[i];
                bool unlocked=p.Recipes==null||p.Recipes.Contains(i);
                bool nearBeacon=i<7||p.Pos.Length<=5;
                bool supplied=true;
                for(int j=0;j<recipe.Cost.Length;j++)supplied&=p.Bag[j]>=recipe.Cost[j];
                bool can=unlocked&&nearBeacon&&supplied&&!p.Ghost&&!p.Extracted;
                Rect card=GUILayoutUtility.GetRect(190,102,GUILayout.ExpandWidth(true));
                PixelPanel(card,can?new Color(.23f,.17f,.36f):FieldPaper);
                DrawSpritePreview(new Rect(card.x+8,card.y+7,38,38),i<4?HeldItemSprite(i):Primitive(CraftIcons[i]));
                UiLabel(new Rect(card.x+51,card.y+8,card.width-58,31),recipe.Name,label);
                float chipX=card.x+10, chipY=card.y+45;
                for(int j=0;j<recipe.Cost.Length;j++)
                {
                    int cost=recipe.Cost[j];
                    if(cost<=0)continue;
                    PixelArt(new Rect(chipX,chipY,17,17),ResourceIcons[j]);
                    var count=new Rect(chipX+19,chipY-2,44,22);
                    var previous=GUI.contentColor;
                    if(p.Bag[j]<cost)GUI.contentColor=new Color(1,.60f,.64f);
                    UiLabel(count,p.Bag[j]+"/"+cost,small);
                    GUI.contentColor=previous;
                    chipX+=60;
                }
                string action=!unlocked?L("Mở công thức ở sảnh","Unlock recipe in Sanctuary"):
                    !nearBeacon?L("Cần đến gần hải đăng","Move closer to the beacon"):
                    !supplied?L("Thiếu nguyên liệu","Missing ingredients"):L("Chế tạo","Craft");
                GUI.enabled=can;
                if(UiButton(new Rect(card.x+8,card.y+69,card.width-16,26),action))intent.Craft=i;
                GUI.enabled=true;
                GUILayout.Space(7);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            hudCapturing=prevCapture;
        }
        void DrawMenu()
        {
            WithSanctuaryTheme(DrawSanctuary);
        }

        void DrawSanctuary()
        {
            DrawSanctuaryBackdrop();
            float width=Mathf.Min(Screen.width-48,1120);
            float x=(Screen.width-width)/2;
            if(sanctuaryBrandSubtitle==null)
            {
                sanctuaryBrandSubtitle=new GUIStyle(label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
            }
            PixelArt(new Rect(0,12,Screen.width,78),EmberLanguage.English?"wordmark_en":"wordmark_vi");
            UiLabel(new Rect(0,92,Screen.width,24),L("Sảnh giữ lửa","Sanctuary"),sanctuaryBrandSubtitle);
            bool headerCurrencies=width>=940;
            if(headerCurrencies)DrawSanctuaryHeaderCurrencies(x,width);
            bool wide=width>=800;
            float stage=wide?width*.40f:0;
            if(wide)DrawSanctuaryStage(new Rect(x,130,stage,Screen.height-156));
            float offset=wide?stage+16:0;
            var content=new Rect(x+offset,130,width-offset,Screen.height-156);
            PixelPanel(content,Paper);
            GUILayout.BeginArea(new Rect(content.x+18,content.y+15,content.width-36,content.height-30));
            ApplySanctuaryNavigation();
            if(!headerCurrencies)DrawSanctuaryCompactCurrencies();
            int page=DrawSanctuaryTabs();
            if(page!=sanctuaryPage)pendingSanctuaryPage=page;
            int currentPage=sanctuaryPage;
            GUILayout.Space(8);
            DrawSanctuarySubtabs(currentPage);
            GUILayout.Space(8);
            metaScroll=GUILayout.BeginScrollView(metaScroll);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            if(currentPage==0)
            {
            DrawCharacterCheat();
            UiText(L("Tên hiển thị","Display name"),label);
            string nextName=GUILayout.TextField(nameText,20);
            if(nextName!=nameText)
            {
                nameText=nextName;
                if(!debugSession)PlayerPrefs.SetString(DisplayNamePref,nameText);
            }
            UiText(L("Bạn bè có thể đặt trùng tên. Trong trận, mỗi người có số P riêng.",
                "Friends can share a name. Each player has a separate P number in the match."),small);
            GUILayout.Space(8);
            DrawVesselPicker();
            if(selected==(int)Vessel.LittleMoone)
            {
                if(!Profile.Vessels.Contains(selected))UiText("Tỷ lệ triệu hồi: 1%",small);
                else if(!Profile.LittleMooneAwake)
                {
                    UiText("Ngủ đông",small);
                    UiText(EmberLanguage.Text("Đá Ký Ức Thức Tỉnh")+": "+Profile.AwakeningStones,small);
                    GUI.enabled=Profile.AwakeningStones>0;
                    if(UiButton("Thức tỉnh Little Moone")&&Profile.AwakenLittleMoone())SaveProfile();
                    GUI.enabled=true;
                }
            }
            GUILayout.Space(10);
            UiText(L("Độ khó · Năng lượng trụ","Difficulty · Beacon energy"),small);
            var nextDifficulty=(Difficulty)DrawSpacedChoices((int)selectedDifficulty,new[]{
                L("Dễ · 500","Easy · 500"),
                L("Trung bình · 750","Normal · 750"),
                L("Khó · 1000","Hard · 1000")
            },38);
            if(nextDifficulty!=selectedDifficulty)
            {
                selectedDifficulty=nextDifficulty;
                if(!debugSession){PlayerPrefs.SetInt(DifficultyPref,(int)selectedDifficulty);PlayerPrefs.Save();}
            }
            GUILayout.Space(8);
            DrawSavedRunCard();
            DrawCoopResumeCard();
            GUI.enabled=Profile.CanPlay(selected);
            if(UiButton(savedRunPreview!=null?L("Bắt đầu trận mới (Solo)","Start new solo run"):L("Chơi một mình","Play solo"),GUILayout.Height(PlayActionHeight)))StartSolo();
            GUI.enabled=true;
            GUILayout.Space(8);
            if(UiButton(onlineExpanded?L("▾ Chơi cùng bạn","▾ Multiplayer"):L("▸ Chơi cùng bạn","▸ Multiplayer"),GUILayout.Height(PlayActionHeight)))onlineExpanded=!onlineExpanded;
            if(onlineExpanded)DrawMultiplayerSetup();
            if(State!=null&&State.Phase!=Phase.Sanctuary&&State.Phase!=Phase.Won&&State.Phase!=Phase.Lost)if(UiButton("Tiếp tục"))menu=false;
            }
            else if(currentPage==1)DrawSummoning();
            else if(currentPage==2)
            {
                if(collectionPage==0)DrawOwnedPortraits();
                else if(collectionPage==1)
                {
                    DrawEquipmentCards(1);
                    DrawEquipmentCards(2);
                }
                else if(collectionPage==2)DrawPerkCards();
                else DrawProgressionExtras();
            }
            else
            {
                if(settingsPage==0)DrawSettings();
                else
                {
            GUILayout.Space(6);UiText("Sao lưu .ember — mật khẩu cá nhân",small);savePassword=GUILayout.PasswordField(savePassword,'•');
            GUILayout.BeginHorizontal();if(UiButton("Xuất .ember"))Export();if(UiButton("Nhập .ember"))Import();if(UiButton("Thư mục"))Application.OpenURL("file:///"+Application.persistentDataPath.Replace('\\','/'));GUILayout.EndHorizontal();DrawBackupVault();
                }
            }
            GUILayout.Space(8);
            UiText(notice,small);
            GUILayout.EndVertical();
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        string LocalPassword=>"Ember-local-v1:"+SystemInfo.deviceUniqueIdentifier;
        string ProfileDirectory=>debugSession?Path.Combine(Application.persistentDataPath,"Sandbox"):Application.persistentDataPath;
        string SavePath=>Path.Combine(Application.persistentDataPath,"profile.ember");
        void LoadProfile()
        {
            if(debugSession){Profile=SandboxProfile();return;}
            Profile=new Profile();if(!File.Exists(SavePath))return;
            try{var loaded=JsonUtility.FromJson<Profile>(EmberSave.Unprotect(File.ReadAllBytes(SavePath),LocalPassword));ValidateProfile(loaded);Profile=loaded;}
            catch(Exception ex){saveBlocked=true;notice="Không đọc được save; giữ nguyên file gốc và khóa ghi tự động. "+ex.Message;}
        }
        static void ValidateProfile(Profile p)
        {
            if(p==null)throw new InvalidDataException("Profile không hợp lệ.");p.ValidateAndNormalize();
            if(p==null||p.Version!=1||p.Ash<0||p.Memory<0||p.Tokens<0||p.PerkSlots<1||p.PerkSlots>3||p.Vessels==null||p.Perks==null||p.Hats==null||p.Pets==null||p.EquippedPerks==null||p.EquippedPerks.Length>p.PerkSlots||p.Vessels.Any(v=>v<0||v>=Catalog.VesselNames.Length)||p.EquippedPerks.Any(v=>!p.Perks.Contains(v)))throw new InvalidDataException("Profile không hợp lệ.");
            if(!p.CanPlay(p.SelectedVessel)||p.Perks.Any(v=>v<0||v>=Catalog.PerkNames.Length)||p.Hats.Any(v=>v<0||v>3)||p.Pets.Any(v=>v<0||v>3)||(p.SelectedHat!=-1&&!p.Hats.Contains(p.SelectedHat))||(p.SelectedPet!=-1&&!p.Pets.Contains(p.SelectedPet)))throw new InvalidDataException("Profile chứa lựa chọn không hợp lệ.");
        }
        void SaveProfile(){if(saveBlocked||debugSession)return;try{Profile.SelectedVessel=Profile.CanPlay(selected)?selected:0;EmberSave.WriteAtomic(SavePath,EmberSave.Protect(JsonUtility.ToJson(Profile),LocalPassword));}catch(Exception ex){notice="Lưu thất bại: "+ex.Message;Trace("save.error",ex.Message);}}
        void Export()
        {
            if(savePassword.Length<8){notice="Dùng mật khẩu sao lưu ít nhất 8 ký tự.";return;}
            try{string destination=EmberFileDialog.Choose(true);string backup=EmberSave.Export(ProfileDirectory,destination,JsonUtility.ToJson(Profile),savePassword);saveFile=string.IsNullOrEmpty(destination)?backup:destination;notice=string.IsNullOrEmpty(destination)?"Đã giữ bản sao trong Backups.":"Đã xuất và giữ bản sao trong Backups.";}
            catch(Exception ex){notice=ex.Message;}
        }
        void Import()
        {
            try{string file=EmberFileDialog.Choose(false);if(string.IsNullOrEmpty(file))return;PreviewRestore(File.ReadAllBytes(file),Path.GetFileName(file));}
            catch(Exception ex){notice=ex.Message;}
        }
        void OnApplicationPause(bool paused){if(paused&&!netSmoke){SaveActiveRun(false);SaveProfile();PlayerPrefs.Save();}}
        void OnApplicationQuit(){if(!netSmoke){SaveActiveRun(false);SaveProfile();PlayerPrefs.Save();}try{lan?.Leave();}catch(Exception ex){Debug.LogWarning(ex.Message);}}
        void OnDestroy(){lan?.Dispose();CloseDiagnostics();}
    }
}
