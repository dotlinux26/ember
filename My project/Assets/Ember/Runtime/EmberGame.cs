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
        EmberLan lan;bool network,host=true,craftOpen,ranges=true,menu=true,rewarded,moba,saveBlocked,beaconDepositOpen;int localId=1;
        Camera cam;GameObject mapRoot,entityRoot;float accumulator,sendClock,joinClock;int renderedSeed=int.MinValue;byte[] currentTiles;
        bool[] exploredTiles;int exploredSize;Texture2D minimapTex;Color32[] minimapColors;float minimapUpdateTimer;bool minimapDirty;
        Texture2D fogTexture;Color32[] fogColors;SpriteRenderer fogRenderer;float fogUpdateTimer;bool fogDirty;
        bool IsExplored(V2 pos)
        {
            if(exploredTiles==null||State==null)return true;
            int sz=State.Size;
            int tx=(int)Mathf.Floor(pos.X+sz/2f);
            int ty=(int)Mathf.Floor(pos.Y+sz/2f);
            if(tx<0||tx>=sz||ty<0||ty>=sz)return false;
            return exploredTiles[ty*sz+tx];
        }
        readonly Dictionary<int,SpriteRenderer> visibleTrees=new Dictionary<int,SpriteRenderer>();
        readonly Stack<SpriteRenderer> treePool=new Stack<SpriteRenderer>();
        readonly Dictionary<int,SpriteRenderer> visibleDecor=new Dictionary<int,SpriteRenderer>();
        readonly Stack<SpriteRenderer> decorPool=new Stack<SpriteRenderer>();
        readonly List<int> keepDecor=new List<int>(),dropDecor=new List<int>();
        readonly Dictionary<int,V2> prevPlayerPos=new Dictionary<int,V2>(),prevEnemyPos=new Dictionary<int,V2>(),prevProjPos=new Dictionary<int,V2>();
        readonly EntityInterpolationBuffer jitterBuffer=new EntityInterpolationBuffer();
        uint localInputSeq;
        V2 localPredictedPos;
        bool localPredictedPosInitialized;
        float localDashCD,localDashTime;
        V2 localDashDir;
        struct PendingInput{public uint Seq;public float Dt;public V2 Move;public bool Dash;public V2 DashDir;public V2 PredictedPos;}
        readonly List<PendingInput> pendingInputs=new List<PendingInput>(128);
        Phase lastObservedPhase;
        float renderInterp=1f;
        Vector2Int visibleChunk=new Vector2Int(int.MinValue,int.MinValue);
        int lastTreeRespawnCount=-1;
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
            if(spriteMaterial==null||spriteMaterial.shader==null||!spriteMaterial.shader.isSupported)
            {
                var litShader=Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                if(litShader!=null&&litShader.isSupported)
                    spriteMaterial=new Material(litShader);
                else
                {
                    var defShader=Shader.Find("Sprites/Default");
                    if(defShader!=null)spriteMaterial=new Material(defShader);
                }
            }
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
            WarmVesselSprites();
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
            World=new EmberWorld(seed,250,selectedDifficulty){DebugEnabled=debugSession};World.S.Capacity=roomCapacity;World.S.RoomName=roomName;World.AddPlayer(1,nameText,(Vessel)selected,Profile.EquippedPerks.Take(Profile.PerkSlots).ToArray(),Profile.SelectedHat,Profile.SelectedPet,Profile.PerkRanks,Profile.Recipes,installationId,awake:Profile.IsAwake(selected));
            World.S.Charge=50; // Starter beacon energy for responsive visual fill and safe zone from match start
            heldSlot=6;lastServerHeldSlot=6;selectedItem=0;inventoryOpen=false;pauseConfirm=false;pauseSettings=false;for(int qi=0;qi<quickSlots.Length;qi++)quickSlots[qi]=0;
            var hostPlayer=World.PlayerById(1);if(hostPlayer!=null)hostPlayer.HeldSlot=6;
            State=World.S;State.Sandbox=debugSession;rewarded=false;accumulator=0;renderedSeed=int.MinValue;snapshotSequence=0;path.Clear();animationState.Clear();prevPlayerPos.Clear();prevEnemyPos.Clear();prevProjPos.Clear();enemyFacingLeft.Clear();ClearActions();
        }
        void StartSolo(){lan?.Leave();network=false;host=true;localId=1;ClearSavedRun();NewWorld(Environment.TickCount);World.S.Capacity=1;World.StartRun(false,liveMatch:true);runAutoSaveClock=RunAutoSaveInterval;menu=false;SaveActiveRun(false,asyncWrite:true);}
        void HostGame()
        {
            try{lan?.Leave();lan=new EmberLan{Capacity=roomCapacity,Mode=routeMode,Trace=Trace};
                if(usePublicBroker){const string chars="ABCDEFGHJKLMNPQRSTUVWXYZ23456789";var bytes=EmberCrypto.Random(6);if(!fixedRoomCode)joinRoomCode=new string(bytes.Select(b=>chars[b%chars.Length]).ToArray());lan.OpenPublic(true,brokerAddress,joinRoomCode,password);}else lan.Open(true,"",password);
                network=true;host=true;localId=1;NewWorld(Environment.TickCount);
                lan.CanRejoin=json=>{try{var info=JsonUtility.FromJson<JoinInfo>(json);return info!=null&&!(info.Sandbox&&!debugSession)&&World!=null&&World.CanRejoin(info.PlayerId);}catch{return false;}};
                lan.Joined=(id,json)=>{var info=JsonUtility.FromJson<JoinInfo>(json);if(info==null||(info.Sandbox&&!debugSession))throw new InvalidDataException();if(World.CanRejoin(info.PlayerId)){World.RejoinPlayer(id,info.PlayerId);return;}World.AddPlayer(id,info.Name,(Vessel)Mathf.Clamp(info.Vessel,0,Catalog.VesselNames.Length-1),(info.Perks??Array.Empty<int>()).Where(p=>p>=0&&p<7).Distinct().Take(3).ToArray(),Mathf.Clamp(info.Hat,-1,3),Mathf.Clamp(info.Pet,-1,3),(info.Ranks??new int[7]).Take(7).Select(r=>Mathf.Clamp(r,0,3)).ToArray(),(info.Recipes??Enumerable.Range(0,7).ToArray()).Where(r=>r>=0&&r<14).Distinct().Take(14).ToArray(),info.PlayerId,awake:info.Awake);};
                lan.Left=id=>World.RemovePlayer(id);lan.Input=(id,json)=>World.Submit(id,JsonUtility.FromJson<Command>(json));lan.CommandInput=(id,cmd)=>World.Submit(id,cmd);
                lan.ReadyChanged=(id,ready)=>World.SetReady(id,ready);
                menu=false;notice="Phòng đã mở. Có thể bắt đầu với 1 người hoặc chờ bạn bè.";
            }catch(Exception ex){notice=ex.Message;}
        }
        void JoinGame()
        {
            try{lan?.Leave();lan=new EmberLan{Mode=routeMode,Trace=Trace};
                if(usePublicBroker){joinRoomCode=joinRoomCode.Trim().ToUpperInvariant();if(joinRoomCode.Length!=6)throw new ArgumentException("Mã phòng gồm 6 ký tự.");lan.OpenPublic(false,brokerAddress,joinRoomCode,password);}else lan.Open(false,address,password);
                network=true;host=false;localId=0;menu=false;State=null;readyWanted=false;
                prevPlayerPos.Clear();prevEnemyPos.Clear();prevProjPos.Clear();
                teleportFadeTimer=0f;localPredictedPosInitialized=false;pendingInputs.Clear();jitterBuffer.Clear();
                SaveLastCoopSession();
                lan.BinaryState=data=>
                {
                    uint ackedSeq;
                    if(EmberWorld.UnpackDelta(data,0,data.Length,ref State,out ackedSeq))
                    {
                        localId=lan.LocalId;
                        if(World==null&&State!=null)
                        {
                            World=new EmberWorld(State.Seed,State.Size,State.Difficulty);
                            World.S=State;
                        }
                        jitterBuffer.Push(Time.realtimeSinceStartupAsDouble,State,localId);
                        if(Local!=null)ReconcileServerState(Local,ackedSeq);
                    }
                };
                lan.State=json=>
                {
                    var snapshot=JsonUtility.FromJson<Snapshot>(json);
                    if(snapshot?.Players!=null)
                    {
                        if(snapshot.Sandbox&&!debugSession)
                        {
                            debugSession=true;Profile=SandboxProfile();
                            Trace("debug.remote_sandbox","host test room: local profile writes disabled");
                        }
                        bool firstState=State==null;
                        State=snapshot;
                        localId=lan.LocalId;
                        if(World==null)
                        {
                            World=new EmberWorld(State.Seed,State.Size,State.Difficulty);
                            World.S=State;
                        }
                        if(firstState&&Local!=null&&Local.HeldSlot>=1&&Local.HeldSlot<=8)heldSlot=Local.HeldSlot;
                        jitterBuffer.Push(Time.realtimeSinceStartupAsDouble,State,localId);
                    }
                };
                joinClock=0;notice="Đang kết nối LAN/VPN...";
            }catch(Exception ex){notice=ex.Message;}
        }
        void ReconcileServerState(Player serverLocal,uint ackedSeq)
        {
            if(serverLocal==null)return;
            if(!localPredictedPosInitialized)
            {
                localPredictedPos=serverLocal.Pos;
                localPredictedPosInitialized=true;
                pendingInputs.Clear();
                return;
            }
            int ackIndex=pendingInputs.FindIndex(p=>p.Seq==ackedSeq);
            if(ackIndex>=0)
            {
                var ackedInput=pendingInputs[ackIndex];
                float dev=V2.Distance(serverLocal.Pos,ackedInput.PredictedPos);
                if(dev>0.05f)
                {
                    V2 replayPos=serverLocal.Pos;
                    float speed=Local!=null?Local.Stats.MoveSpeed:6f;
                    for(int i=ackIndex+1;i<pendingInputs.Count;i++)
                    {
                        var cmd=pendingInputs[i];
                        if(cmd.Dash)replayPos=World!=null?World.Move(replayPos,cmd.DashDir*17*cmd.Dt,Local,ignoreEnemies:true):replayPos+cmd.DashDir*17*cmd.Dt;
                        else replayPos=World!=null?World.Move(replayPos,cmd.Move*speed*cmd.Dt,Local):replayPos+cmd.Move*speed*cmd.Dt;
                        cmd.PredictedPos=replayPos;
                        pendingInputs[i]=cmd;
                    }
                    localPredictedPos=replayPos;
                    if(Local!=null)Local.Pos=localPredictedPos;
                }
                pendingInputs.RemoveRange(0,ackIndex+1);
            }
            else if(ackedSeq>0&&pendingInputs.Count>0&&ackedSeq>=pendingInputs[pendingInputs.Count-1].Seq)
            {
                localPredictedPos=serverLocal.Pos;
                if(Local!=null)Local.Pos=localPredictedPos;
                pendingInputs.Clear();
            }
        }
        [Serializable] sealed class JoinInfo{public string Name,PlayerId;public bool Sandbox;public int Vessel,Hat=-1,Pet=-1;public int[] Perks,Ranks,Recipes;public bool Awake;}
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
        V2 VisualPlayerPos(Player p)
        {
            if(p==null)return default;
            if(network&&!host&&p.Id==localId)
                return localPredictedPosInitialized?localPredictedPos:p.Pos;
            if(network&&!host)
                return jitterBuffer.GetPlayerPos(p.Id,Time.realtimeSinceStartupAsDouble,p.Pos);
            return prevPlayerPos.TryGetValue(p.Id,out var prev)?SmoothPos(prev,p.Pos,renderInterp):p.Pos;
        }
        V2 VisualEnemyPos(Enemy e)
        {
            if(e==null)return default;
            if(network&&!host)
                return jitterBuffer.GetEnemyPos(e.Id,Time.realtimeSinceStartupAsDouble,e.Pos);
            return prevEnemyPos.TryGetValue(e.Id,out var prev)?SmoothPos(prev,e.Pos,renderInterp):e.Pos;
        }
        V2 VisualProjectilePos(Projectile s)
        {
            if(s==null)return default;
            if(network&&!host)
                return jitterBuffer.GetProjectilePos(s.Id,Time.realtimeSinceStartupAsDouble,s.Pos);
            return prevProjPos.TryGetValue(s.Id,out var prev)?SmoothPos(prev,s.Pos,renderInterp):s.Pos;
        }

        void Update()
        {
            EnsureAudio();
            teleportFadeTimer=Mathf.Max(0f,teleportFadeTimer-Time.deltaTime);
            clock+=Time.deltaTime;lan?.Poll(Time.realtimeSinceStartupAsDouble);DiagnosticTick();
            TickCoopAutoReconnect(Time.deltaTime);
            if(network&&!host&&lan.LocalId==0&&string.IsNullOrEmpty(lan.Error)){joinClock-=Time.deltaTime;if(joinClock<=0){lan.Join(JsonUtility.ToJson(new JoinInfo{Name=nameText,PlayerId=installationId,Sandbox=debugSession,Vessel=selected,Perks=Profile.EquippedPerks.Take(Profile.PerkSlots).ToArray(),Ranks=Profile.PerkRanks,Recipes=Profile.Recipes,Hat=Profile.SelectedHat,Pet=Profile.SelectedPet,Awake=Profile.IsAwake(selected)}));joinClock=1;}}
            SampleFrameRate();
            if(Local!=null)
            {
                CheckAutoAssignQuickSlots(Local);
                if(Local.HeldSlot!=lastServerHeldSlot)
                {
                    lastServerHeldSlot=Local.HeldSlot;
                    heldSlot=Local.HeldSlot;
                    if(heldSlot>=2&&heldSlot<=5)selectedItem=heldSlot-2;
                }
                if(heldSlot>=2&&heldSlot<=5&&(Local.Items==null||heldSlot-2>=Local.Items.Length||Local.Items[heldSlot-2]<=0))
                {
                    heldSlot=6;
                    intent.Equip=6;
                }
            }
            ReadInput();
            ReadCameraZoom();
            // Map Editor moved to web-based tool
            if(netSmoke&&!host){intent.X=State?.Phase==Phase.Explore&&State.Time<6?1:0;intent.Y=0;if(lan.LocalId>0&&!readyWanted){readyWanted=true;lan.SetReady(true);}}
            if(netSmoke&&host&&State.Players.Length==netExpected&&State.Players.All(p=>p.Ready))BeginRoomRun();
            if((!menu||network)&&host)
            {
                if(World==null)return;
                accumulator+=Mathf.Min(Time.deltaTime,.1f);
                while(accumulator>=EmberWorld.Step){CapturePrevPositions();World.Submit(localId,intent);World.Tick();accumulator-=EmberWorld.Step;ClearActions();}
                State=World.S;
                renderInterp=Mathf.Clamp01(accumulator/EmberWorld.Step);
                sendClock-=Time.deltaTime;
                if(network&&sendClock<=0)
                {
                    snapshotSequence++;
                    byte[] deltaData=EmberWorld.PackDelta(State,0);
                    lan.BroadcastSnapshot(deltaData);
                    if(snapshotSequence%90==0)lan.Broadcast(JsonUtility.ToJson(State),snapshotSequence);
                    sendClock=1f/30f;
                }
            }
            else if(network&&!host)
            {
                renderInterp=1f;
                if(State!=null&&State.Phase!=lastObservedPhase)
                {
                    lastObservedPhase=State.Phase;
                    prevPlayerPos.Clear();prevEnemyPos.Clear();prevProjPos.Clear();
                    teleportFadeTimer=0f;localPredictedPosInitialized=false;
                    pendingInputs.Clear();jitterBuffer.Clear();
                }
                if(Local!=null&&!Local.Ghost)
                {
                    if(!localPredictedPosInitialized)
                    {
                        localPredictedPos=Local.Pos;
                        localPredictedPosInitialized=true;
                    }
                    float dt=Time.deltaTime;
                    localDashCD=Mathf.Max(0f,localDashCD-dt);
                    V2 inputDir=new V2(intent.X,intent.Y);
                    if(inputDir.Length>1)inputDir=inputDir.Normal;
                    float speed=Local.Stats.MoveSpeed;
                    if(Local.StaminaBuffTimer>0&&!Local.HasBuff(BuffKind.Swiftness))speed*=1.35f;
                    if(Local.HasBuff(BuffKind.Swiftness))speed*=(1f+Local.BuffValue(BuffKind.Swiftness));
                    if(Local.Load>Local.Capacity)speed*=.55f;

                    localInputSeq++;
                    bool doDash=intent.Dash&&localDashCD<=0&&Local.Stamina>=20;
                    V2 newPredicted=localPredictedPos;
                    bool isDashing=false;
                    V2 activeDashDir=localDashDir;
                    if(localDashTime>0)
                    {
                        isDashing=true;
                        localDashTime-=dt;
                        newPredicted=World!=null?World.Move(localPredictedPos,localDashDir*17*dt,Local,ignoreEnemies:true):localPredictedPos+localDashDir*17*dt;
                    }
                    else if(doDash)
                    {
                        localDashDir=EmberWorld.EightWay(inputDir.Length>.1f?inputDir:new V2(Local.Right?1:-1,0));
                        activeDashDir=localDashDir;
                        localDashCD=Local.Stats.DashCooldown;
                        localDashTime=.16f;
                        isDashing=true;
                        newPredicted=World!=null?World.Move(localPredictedPos,localDashDir*17*dt,Local,ignoreEnemies:true):localPredictedPos+localDashDir*17*dt;
                    }
                    else
                    {
                        newPredicted=World!=null?World.Move(localPredictedPos,inputDir*speed*dt,Local):localPredictedPos+inputDir*speed*dt;
                    }
                    localPredictedPos=newPredicted;
                    Local.Pos=localPredictedPos;

                    if(pendingInputs.Count>=120)pendingInputs.RemoveAt(0);
                    pendingInputs.Add(new PendingInput{Seq=localInputSeq,Dt=dt,Move=inputDir,Dash=isDashing,DashDir=activeDashDir,PredictedPos=localPredictedPos});
                }
                sendClock-=Time.deltaTime;
                if(sendClock<=0)
                {
                    if(intent.HasActions&&State?.Phase!=Phase.Sanctuary)lan.SendAction(JsonUtility.ToJson(intent));
                    lan.SendInputBinary(localInputSeq,new Command{X=intent.X,Y=intent.Y,Dash=intent.Dash});
                    sendClock=1f/30f;
                    ClearActions();
                }
            }
            else renderInterp=1f;
            if(State==null)return;
            if(renderedSeed!=State.Seed)BuildMap();
            EnsureFogOfWar();
            Render();
            RenderLighting();
            UpdateAmbientAudio();
            StreamTrees();
            UpdateExploration(Time.deltaTime);
            if(netSmoke&&!netSmokeValidated&&State.Tick>120&&State.Players.Length==netExpected&&State.Players.All(p=>p.Id==1||p.Pos.X>(p.Id-1)*1.5f+1))NetSmokeCheck();
            if(Local!=null&&Local.Dread>=30&&!Local.Safe){phantomClock-=Time.deltaTime;if(phantomClock<=0&&audioSource!=null&&footstep!=null){audioSource.panStereo=Mathf.Sin(clock*2)*.8f;audioSource.PlayOneShot(footstep,.12f);phantomClock=3.5f;}}
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
        void BeginRoomRun()
        {
            if(World.StartRun(true,liveMatch:true))
            {
                lan.AcceptingJoins=false;
                prevPlayerPos.Clear();prevEnemyPos.Clear();prevProjPos.Clear();
                teleportFadeTimer=0f;localPredictedPosInitialized=false;
                pendingInputs.Clear();jitterBuffer.Clear();
                ClearActions();
            }
        }
        void LeaveRoom()
        {
            coopReconnectArmed=false;
            coopReconnectTimer=0;
            coopReconnectAttempts=0;
            ClearLastCoopSession();
            joinRoomCode="";
            try{lan?.Leave();}catch(Exception ex){Debug.LogWarning(ex.Message);}
            network=false;host=true;localId=1;menu=true;State=null;path.Clear();
            prevPlayerPos.Clear();prevEnemyPos.Clear();prevProjPos.Clear();
            teleportFadeTimer=0f;localPredictedPosInitialized=false;
            pendingInputs.Clear();jitterBuffer.Clear();
            ClearActions();
        }
        static AudioClip MakeTone(float hz,float seconds)
        {
            int count=(int)(22050*seconds);var samples=new float[count];for(int i=0;i<count;i++)samples[i]=Mathf.Sin(i*hz*Mathf.PI*2/22050)*(1-(float)i/count)*.2f;
            var clip=AudioClip.Create("Ember tone",count,1,22050,false);clip.SetData(samples,0);return clip;
        }
        AudioClip warpTone;
        float teleportFadeTimer;
        void TriggerTeleportFade()
        {
            teleportFadeTimer=.55f;
            path.Clear();
            intent.X=intent.Y=0;
            if(audioSource!=null&&warpTone!=null)audioSource.PlayOneShot(warpTone,.35f);
        }
        float GetTeleportFadeAlpha()
        {
            if(teleportFadeTimer<=0f)return 0f;
            if(teleportFadeTimer>.38f)return Mathf.Clamp01((.55f-teleportFadeTimer)/.17f)*.95f;
            if(teleportFadeTimer>.30f)return .95f;
            return Mathf.Clamp01(teleportFadeTimer/.30f)*.95f;
        }
        void EnsureAudio()
        {
            if(audioSource==null)audioSource=GetComponent<AudioSource>()??gameObject.AddComponent<AudioSource>();
            if(tone==null)tone=MakeTone(440,.12f);
            if(footstep==null)footstep=MakeTone(90,.08f);
            if(warpTone==null)warpTone=MakeTone(180,.28f);
        }
        bool Press(Key key)=>Keyboard.current!=null&&Keyboard.current[key].wasPressedThisFrame;
        bool Held(Key key)=>Keyboard.current!=null&&Keyboard.current[key].isPressed;
        void ReadInput()
        {
            if(Press(Key.Escape))
            {
                if(EmberMerchant.DialogOpen)
                {
                    EmberMerchant.DialogOpen=false;
                }
                else if(inventoryOpen)
                {
                    if(pendingQuickAssign>0)pendingQuickAssign=0;
                    else {inventoryOpen=false;dragFromCell=-1;draggingItem=false;}
                }
                else if(beaconDepositOpen)beaconDepositOpen=false;
                else if(craftOpen)craftOpen=false;
                else if(pauseConfirm||pauseSettings){pauseConfirm=false;pauseSettings=false;}
                else {menu=!menu;pauseConfirm=false;pauseSettings=false;if(menu)SaveActiveRun(false,asyncWrite:true);}
                path.Clear();
            }
            if(menu||debugOpen||State==null||State.Phase==Phase.Sanctuary||(network&&!host&&!string.IsNullOrEmpty(lan?.Error))){intent.X=intent.Y=0;return;}
            if(Local!=null&&Local.Ghost)
            {
                intent.X=intent.Y=0;inventoryOpen=false;craftOpen=false;beaconDepositOpen=false;path.Clear();
                if(Press(Key.A)||Press(Key.LeftArrow)||Press(Key.Q))CycleSpectate(-1);
                if(Press(Key.D)||Press(Key.RightArrow)||Press(Key.E)||Press(Key.Space))CycleSpectate(1);
                return;
            }
            if(Press(Key.I)){inventoryOpen=!inventoryOpen;if(!inventoryOpen){pendingQuickAssign=0;dragFromCell=-1;draggingItem=false;}craftOpen=false;beaconDepositOpen=false;path.Clear();}
            if(Press(Key.Tab)){craftOpen=!craftOpen;inventoryOpen=false;beaconDepositOpen=false;pendingQuickAssign=0;}
            if(Press(Key.Digit1)||Press(Key.Numpad1)){if(inventoryOpen&&pendingQuickAssign>0)BindQuickSlot(0,pendingQuickAssign);else EquipSlot(quickSlots[0]);}
            if(Press(Key.Digit2)||Press(Key.Numpad2)){if(inventoryOpen&&pendingQuickAssign>0)BindQuickSlot(1,pendingQuickAssign);else EquipSlot(quickSlots[1]);}
            if(Press(Key.Digit3)||Press(Key.Numpad3)){if(inventoryOpen&&pendingQuickAssign>0)BindQuickSlot(2,pendingQuickAssign);else EquipSlot(quickSlots[2]);}
            if(Press(Key.Digit4)||Press(Key.Numpad4)){if(inventoryOpen&&pendingQuickAssign>0)BindQuickSlot(3,pendingQuickAssign);else EquipSlot(quickSlots[3]);}
            if(Press(Key.B)&&beaconDepositOpen){beaconDepositOpen=false;}
            if(inventoryOpen||beaconDepositOpen){intent.X=intent.Y=0;return;}
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
            if(moba&&Mouse.current!=null&&!overUI&&Local!=null&&teleportFadeTimer<=.25f)
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
            bool wantAttack=(Held(Key.K)||(Mouse.current!=null&&(Mouse.current.leftButton.isPressed||Mouse.current.leftButton.wasPressedThisFrame)&&!overUI))&&Local!=null&&!Local.Ghost;
            bool isNearShroom=State?.Nodes!=null&&State.Nodes.Any(n=>!n.Dropped&&n.DropKind==2&&n.DropId==16&&V2.Distance(n.Pos,Local.Pos)<2.5f);
            bool attack=wantAttack&&(IsHoldingWeaponSlot(heldSlot)||isNearShroom||heldSlot==6)&&Local.AttackCD<=0&&!Local.Reloading;
            intent.Attack|=attack;if(attack&&audioSource!=null&&tone!=null)audioSource.PlayOneShot(tone,.2f);
            bool wantUse=(Press(Key.K)||(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame&&!overUI));
            if((wantUse||Press(Key.U))&&CanUseSelectedItem(Local))intent.Use=selectedItem;
            bool pressedInteract=Press(Key.E);
            if(pressedInteract&&Local!=null&&!Local.Ghost)
            {
                if(EmberMerchant.DialogOpen)
                {
                    EmberMerchant.DialogOpen=false;
                    pressedInteract=false;
                }
                else
                {
                    var merchant=State?.Structures?.FirstOrDefault(s=>EmberMerchant.IsMerchant(s)&&V2.Distance(s.Pos,Local.Pos)<=EmberMerchant.MerchantInteractionDistance);
                    bool hasNearbyStructure=(State?.Structures!=null&&State.Structures.Any(s=>((s.Kind==0&&s.Life>0)||s.Kind==2||s.Kind==4||(s.Kind==12&&s.HP>0))&&V2.Distance(s.Pos,Local.Pos)<2.5f))
                        ||(State?.Nodes!=null&&State.Nodes.Any(n=>(n.Dropped||(!n.Dropped&&n.DropKind==2&&n.DropId==16))&&V2.Distance(n.Pos,Local.Pos)<2.5f));
                    if(merchant!=null)
                    {
                        EmberMerchant.DialogOpen=true;
                        inventoryOpen=false;
                        craftOpen=false;
                        beaconDepositOpen=false;
                        pressedInteract=false;
                    }
                    else if(!hasNearbyStructure&&ShouldAutoEquipPickaxe(Local))
                    {
                        AutoEquipHarvestTool(Local);
                    }
                    else if(!hasNearbyStructure&&IsNearTreeOrRock(Local)&&!HasHarvestTool(Local))
                    {
                        pressedInteract=false; // Block interaction when missing tool
                        bool isTree=IsNearTree(Local);
                        notice=isTree
                            ?(EmberLanguage.English?"Need an Axe or Pickaxe to chop trees!":"Không có Rìu hoặc Cuốc để chặt cây!")
                            :(EmberLanguage.English?"Need a Pickaxe to mine rocks!":"Không có Cuốc để khai thác quặng!");
                    }
                }
            }
            bool pressedSkill=Press(Key.J);
            if(pressedSkill)
            {
                intent.Skill=true;
                path.Clear();
                intent.X=intent.Y=0;
                if(Local!=null&&Local.Vessel==Vessel.SpaceWraith&&Local.SkillCD<=0)
                    TriggerTeleportFade();
            }
            intent.Dash|=Press(Key.Space);intent.Interact|=pressedInteract;intent.Refuel|=Press(Key.Q);
            if(Press(Key.B))
            {
                if(beaconDepositOpen)
                {
                    beaconDepositOpen=false;
                }
                else if(Local!=null&&Local.Pos.Length<4.5f)
                {
                    beaconDepositOpen=true;
                    craftOpen=false;
                    inventoryOpen=false;
                    if(EmberMerchant.DialogOpen)EmberMerchant.DialogOpen=false;
                }
                else
                {
                    notice=L("Hãy lại gần Hải Đăng để nạp năng lượng!","Approach the Beacon to deposit energy!");
                }
            }
            intent.ToggleLamp|=Press(Key.T);intent.PassLamp|=Press(Key.G);intent.Push|=Press(Key.L);intent.Revive|=Press(Key.H);intent.Reload|=Press(Key.X);
            if(Local!=null&&Local.Pos.Length<4.5f)
            {
                if(Press(Key.Z))intent.Use=10; // Brew Lumidust Tea
                else if(Press(Key.C))intent.Use=11; // Polish Pickaxe
            }
            ApplyTouchInput();
        }
        void ClearActions(){intent.Attack=intent.Skill=intent.Dash=intent.Interact=intent.Refuel=intent.Deposit=intent.ToggleLamp=intent.PassLamp=intent.Push=intent.Revive=intent.Reload=false;intent.Craft=intent.Use=-1;intent.Equip=0;intent.DepositRes=-1;intent.DepositAmt=0;}
        bool IsNearTree(Player p)
        {
            if(p==null||currentTiles==null||State==null)return false;
            int size=State.Size;
            int px=(int)Math.Floor(p.Pos.X+size/2f),py=(int)Math.Floor(p.Pos.Y+size/2f);
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
            {
                int tx=px+dx,ty=py+dy;
                if(tx<0||ty<0||tx>=size||ty>=size)continue;
                byte t=currentTiles[ty*size+tx];
                if(t==3)
                {
                    V2 blockPos=new V2(tx-size/2f+.5f,ty-size/2f+.5f);
                    if(V2.Distance(p.Pos,blockPos)<2.6f)return true;
                }
            }
            return State.Nodes!=null&&State.Nodes.Any(n=>!n.Dropped&&n.Resource==Resource.Wood&&V2.Distance(n.Pos,p.Pos)<2.4f);
        }
        bool IsNearTreeOrRock(Player p)
        {
            if(p==null||currentTiles==null||State==null)return false;
            int size=State.Size;
            int px=(int)Math.Floor(p.Pos.X+size/2f),py=(int)Math.Floor(p.Pos.Y+size/2f);
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
            {
                int tx=px+dx,ty=py+dy;
                if(tx<0||ty<0||tx>=size||ty>=size)continue;
                byte t=currentTiles[ty*size+tx];
                if(t==3||t==2)
                {
                    V2 blockPos=new V2(tx-size/2f+.5f,ty-size/2f+.5f);
                    if(V2.Distance(p.Pos,blockPos)<2.6f)return true;
                }
            }
            return State.Nodes!=null&&State.Nodes.Any(n=>!n.Dropped&&(n.Resource==Resource.Wood||n.Resource==Resource.Ore)&&V2.Distance(n.Pos,p.Pos)<2.4f);
        }
        bool IsNavWalkable(byte[] tiles,int size,V2 pos)
        {
            int x=Mathf.FloorToInt(pos.X+size/2f),y=Mathf.FloorToInt(pos.Y+size/2f);
            if(x<3||y<3||x>=size-3||y>=size-3)return false;
            byte t=tiles[y*size+x];
            if(t==1||t==3||t==4)return false;
            if(V2.Distance(pos,new V2(0,.35f))<1.1f)return false;
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
            if(dist>35f)return false;
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
                        byte t1=tiles[y*size+nx],t2=tiles[ny*size+x];
                        if(t1==1||t1==3||t1==4||t2==1||t2==3||t2==4)continue;
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
        Sprite DroppedItemSprite(Node n)
        {
            if(n.DropKind==1)
            {
                int wid=n.DropId;
                if(wid==(int)Weapon.Pickaxe||wid==8)return Primitive("pickaxe");
                if(wid>=0&&wid<InventoryWeaponArt.Length)return Primitive(InventoryWeaponArt[wid]);
                return Primitive("stick");
            }
            if(n.DropKind==2)
            {
                switch(n.DropId)
                {
                    case 0: return Primitive("torch");
                    case 1: return Primitive("flare");
                    case 2: return Primitive("sedative");
                    case 3: return Primitive("revival");
                    case 8: return Primitive("pickaxe");
                    case 14: return Primitive("energy_drink");
                    case 15: return Primitive("apple");
                    case 16: return Primitive("mushroom");
                    case 17: return Primitive("roasted_apple");
                    case 18: return Primitive("mushroom_stew");
                    case 19: return Primitive("pickaxe");
                    case 20: return Primitive("stone_pickaxe");
                    case 21: return Primitive("axe");
                    case 22: return Primitive("stick");
                    default:
                        if(n.DropId==8)return Primitive("pickaxe");
                        return Primitive("diamond");
                }
            }
            if(n.Dropped&&n.DropId==(int)Weapon.Pickaxe)return Primitive("pickaxe");
            return n.Resource==Resource.Wood?Primitive("wood"):n.Resource==Resource.Ore?Primitive("ore"):MaterialSprite(ResourceIcons[(int)n.Resource]);
        }
        Sprite Primitive(string name)
        {
            if(primitives.TryGetValue(name,out var cached))return cached;
            var authored=Resources.Load<Texture2D>("Ember/Props/"+name);
            if(authored!=null)
            {
                authored.filterMode=FilterMode.Point;
                bool rooted=name.StartsWith("enemy_")||name=="nest"||name=="boss"||name.StartsWith("tree")||name.StartsWith("rock")||name.StartsWith("bush")||name.StartsWith("log")||name=="wall"||name=="chest"||name=="torch"||name=="torch_ground"||name=="merchant"||name.StartsWith("abandoned_chest")||name.StartsWith("stone_")||name.EndsWith("stump")||name=="wildflowers"||name=="mushrooms";
                bool isAnimStrip=name.StartsWith("enemy_")||name.StartsWith("boss")||name=="merchant";
                int frameW=(authored.width>authored.height&&isAnimStrip)?authored.height:authored.width;
                cached=Sprite.Create(authored,new Rect(0,0,frameW,authored.height),new Vector2(.5f,rooted?.05f:.5f),32);
                primitives[name]=cached;
                return cached;
            }
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
                    case "apple":
                        if(y>=12&&((x>=9&&x<=11&&y==12)||(x>=10&&x<=12&&y==13))){draw=true;color=(x==11&&y==13)?new Color(.5f,.9f,.5f):new Color(.18f,.5f,.22f);}
                        else if(y>=11&&y<=13&&(x==7||x==8)){draw=true;color=new Color(.42f,.26f,.18f);}
                        else if(r<5.4f&&y<=11){draw=true;bool isSpec=(x==5||x==6)&&(y==8||y==9);bool isBounce=y<=3&&x>=8;color=isSpec?Color.white:isBounce?new Color(.92f,.65f,.25f):x<7?new Color(.88f,.22f,.32f):new Color(.58f,.12f,.22f);}
                        break;
                    case "mushroom": case "mushrooms":
                        float elip=(x-7.5f)*(x-7.5f)/(6.2f*6.2f)+(y-8.5f)*(y-8.5f)/(4.8f*4.8f);
                        if(elip<1f&&y>=7){draw=true;bool isDot=(x==5&&y==11)||(x==9&&y==11)||(x==7&&y==12)||(x==8&&y==9)||(x==4&&y==8);color=isDot?new Color(1f,.95f,.8f):y==7?new Color(.25f,.2f,.35f):x<7?new Color(.85f,.2f,.32f):new Color(.55f,.12f,.22f);}
                        else if(x>=6&&x<=9&&y>=2&&y<7){draw=true;color=x==6?new Color(.95f,.92f,.88f):x<=8?new Color(.82f,.78f,.72f):new Color(.52f,.46f,.4f);}
                        else if(x>=5&&x<=10&&y<2){draw=true;color=new Color(.15f,.35f,.2f);}
                        break;
                    case "energy_drink":
                        if(y>=12&&y<=14&&x>=7&&x<=8){draw=true;color=new Color(.55f,.35f,.22f);}
                        else if(y==11&&x>=6&&x<=9){draw=true;color=new Color(.95f,.72f,.22f);}
                        else if(y>=9&&y<=10&&x>=7&&x<=8){draw=true;color=new Color(.45f,.8f,.92f);}
                        else if(Vector2.Distance(new Vector2(x,y),new Vector2(7.5f,4.5f))<4.8f){draw=true;bool isSpec=x==4||(x==5&&y>=6);bool isCore=(x==7&&y==4)||(x==8&&y==5);color=isSpec?Color.white:isCore?new Color(1f,.92f,.35f):x<7?new Color(.45f,.85f,.95f):new Color(.18f,.55f,.72f);}
                        break;
                    case "merchant":
                        if(y>=10&&y<=14&&x>=5&&x<=10){draw=true;color=y>=12?new Color(.32f,.38f,.48f):new Color(.08f,.07f,.12f);}
                        else if(y==9&&(x==6||x==8)){draw=true;color=new Color(1f,.85f,.3f);}
                        else if(y==7&&x>=5&&x<=10){draw=true;color=new Color(.65f,.18f,.28f);}
                        else if(x>=2&&x<=4&&y>=3&&y<=12){draw=true;color=y>=10?new Color(.75f,.6f,.45f):new Color(.42f,.26f,.18f);}
                        else if(x>=5&&x<=11&&y>=2&&y<=6){draw=true;color=y==2?new Color(.9f,.7f,.2f):x<=7?new Color(.32f,.38f,.48f):new Color(.2f,.24f,.32f);}
                        else if(x>=12&&x<=14&&y>=3&&y<=7){draw=true;color=(x==13&&y>=4&&y<=6)?new Color(1f,.85f,.35f):new Color(.25f,.18f,.12f);}
                        else if(y<2&&(x==6||x==7||x==9||x==10)){draw=true;color=new Color(.25f,.16f,.12f);}
                        break;
                    case "roasted_apple":
                        if(r<5.2f&&y<=11){draw=true;bool isGlaze=(x==6||x==8)&&(y==5||y==7);color=isGlaze?new Color(.35f,.18f,.1f):new Color(.92f,.55f,.18f);}
                        else if(y>=11&&y<=13&&(x==7||x==8)){draw=true;color=new Color(.35f,.18f,.1f);}
                        break;
                    case "mushroom_stew":
                        if(y>=2&&y<=5&&x>=3&&x<=12){draw=true;color=new Color(.45f,.28f,.18f);}
                        else if(y>=6&&y<=8&&x>=4&&x<=11){draw=true;color=(x==6||x==8)?new Color(1f,.95f,.8f):new Color(.92f,.65f,.25f);}
                        else if(y>=9&&y<=12&&(x==9||x==10)){draw=true;color=new Color(.65f,.45f,.3f);}
                        break;
                    case "abandoned_chest":
                        draw = x>=3 && x<=12 && y>=2 && y<=13;
                        bool isLock = (x==7 || x==8) && (y==6 || y==7);
                        bool isIronBand = x==4 || x==11 || y==2 || y==3 || y==12;
                        color = isLock ? new Color(.47f,.84f,.91f) : isIronBand ? new Color(.32f,.38f,.48f) : new Color(.42f,.26f,.18f);
                        break;
                    case "abandoned_chest_open":
                        draw = (x>=3 && x<=12 && y>=2 && y<=10) || (x>=4 && x<=11 && y>=11 && y<=14);
                        bool isOpenInner = x>=5 && x<=10 && y>=6 && y<=9;
                        color = isOpenInner ? new Color(.08f,.07f,.12f) : (x==4||x==11||y==2) ? new Color(.32f,.38f,.48f) : new Color(.42f,.26f,.18f);
                        break;
                    case "lantern_inferno":
                        draw = (x>=4 && x<=11 && y>=2 && y<=12) || (x>=6 && x<=9 && y>=13 && y<=14);
                        if(draw)
                        {
                            bool isFrame = x==4 || x==11 || y==2 || y==3 || y==11 || y>=12;
                            bool isCoreFire = x>=6 && x<=9 && y>=5 && y<=8;
                            color = isFrame ? new Color(.16f, .14f, .22f) : isCoreFire ? new Color(1f, .92f, .45f) : new Color(1f, .38f, .15f);
                        }
                        break;
                    case "lantern_relic":
                        draw = (x>=4 && x<=11 && y>=2 && y<=12) || (x>=6 && x<=9 && y>=13 && y<=15);
                        if(draw)
                        {
                            bool isFrame = x==4 || x==11 || y==2 || y==3 || y==11 || y>=12;
                            bool isCrystalCore = (x==7 || x==8) && (y>=5 && y<=8);
                            color = isFrame ? new Color(.82f, .65f, .28f) : isCrystalCore ? Color.white : new Color(.35f, .85f, .95f);
                        }
                        break;
                    case "axe":
                        if(y>=10&&y<=14&&x>=9&&x<=14)
                        {
                            draw=true;
                            bool isEdge=x==13||x==14;
                            bool isHi=y>=11&&y<=13&&x>=12;
                            color=isHi?Color.white:isEdge?new Color(.42f,.49f,.60f):new Color(.23f,.27f,.35f);
                        }
                        else if(y>=9&&y<=13&&x>=6&&x<=8)
                        {
                            draw=true;
                            color=new Color(.15f,.18f,.24f);
                        }
                        else if(y>=2&&y<=12&&(x==7||x==8))
                        {
                            draw=true;
                            color=x==7?new Color(.42f,.28f,.22f):new Color(.60f,.41f,.31f);
                        }
                        break;
                    case "pickaxe":
                        if(y>=10&&y<=14&&x>=3&&x<=13)
                        {
                            bool inArch=(y==12&&x>=4&&x<=12)||(y==11&&(x==3||x==4||x==12||x==13))||(y==13&&x>=7&&x<=9);
                            if(inArch)
                            {
                                draw=true;
                                bool isTip=x==3||x==13;
                                bool isHi=(x>=7&&x<=9)&&y==12;
                                color=isHi?Color.white:isTip?new Color(.42f,.49f,.60f):new Color(.23f,.27f,.35f);
                            }
                        }
                        else if(y>=2&&y<=11&&(x==7||x==8))
                        {
                            draw=true;
                            color=x==7?new Color(.42f,.28f,.22f):new Color(.60f,.41f,.31f);
                        }
                        break;
                    case "torch":
                        if(y>=10&&y<=14&&x>=5&&x<=11)
                        {
                            draw=true;
                            bool isCore=(x==7||x==8)&&(y==11||y==12);
                            bool isHi=x==8&&y==12;
                            bool isCyan=x==10&&y==14;
                            color=isHi?Color.white:isCore?new Color(1f,.94f,.70f):isCyan?new Color(.47f,.84f,.91f):x<8?new Color(.97f,.71f,.22f):new Color(.85f,.48f,.14f);
                        }
                        else if(y>=8&&y<=10&&x>=6&&x<=9)
                        {
                            draw=true;
                            color=new Color(.24f,.16f,.13f);
                        }
                        else if(y>=2&&y<=8&&(x==7||x==8))
                        {
                            draw=true;
                            color=x==7?new Color(.42f,.28f,.22f):new Color(.60f,.41f,.31f);
                        }
                        break;
                    case "flare":
                        if(y>=11&&y<=15&&x>=5&&x<=11)
                        {
                            draw=true;
                            bool isSpark=Math.Abs(x-8)+Math.Abs(y-13)<=2;
                            color=isSpark?Color.white:x>8?new Color(.47f,.84f,.91f):new Color(1f,.94f,.70f);
                        }
                        else if(y>=3&&y<=10&&(x==7||x==8))
                        {
                            draw=true;
                            color=x==7?new Color(.85f,.48f,.14f):new Color(.97f,.71f,.22f);
                        }
                        break;
                    case "sedative":
                        if(y>=12&&y<=14&&x>=7&&x<=8)
                        {
                            draw=true;
                            color=new Color(.42f,.49f,.60f);
                        }
                        else if(Vector2.Distance(new Vector2(x,y),new Vector2(7.5f,6.5f))<4.8f)
                        {
                            draw=true;
                            bool isSpec=x==5&&(y==7||y==8);
                            bool isBubble=(x==8&&y==5)||(x==9&&y==7);
                            color=isSpec?Color.white:isBubble?new Color(.47f,.84f,.91f):x<8?new Color(.55f,.17f,.24f):new Color(.35f,.11f,.17f);
                        }
                        break;
                    case "revival":
                        int manhattan=Math.Abs(x-8)+Math.Abs(y-8);
                        if(manhattan<=6)
                        {
                            draw=true;
                            bool isCore=manhattan<=2;
                            color=isCore?new Color(1f,.94f,.70f):x<8?new Color(.47f,.84f,.91f):new Color(.32f,.37f,.48f);
                        }
                        break;
                    case "torch_ground":
                        if(y>=2&&y<=12&&(x==7||x==8)){draw=true;color=x==7?new Color(.42f,.28f,.22f):new Color(.60f,.41f,.31f);}
                        else if(y>=10&&y<=13&&x>=6&&x<=9){draw=true;color=new Color(.15f,.18f,.24f);}
                        break;
                    case "torch_fire":
                        float fDist=Vector2.Distance(new Vector2(x,y),new Vector2(7.5f,7.5f));
                        if(fDist<5.2f){draw=true;color=fDist<2.2f?Color.white:fDist<3.8f?new Color(1f,.94f,.45f):new Color(.95f,.52f,.18f);}
                        break;
                    default:draw=x>=2&&x<14&&y>=2&&y<14;break;
                }
                if(draw)c[y*size+x]=color;
            }
            t.SetPixels32(c);t.Apply();cached=Sprite.Create(t,new Rect(0,0,size,size),new Vector2(.5f,.5f),16);primitives[name]=cached;return cached;
        }
        AnimationClip LoadCharacterAnim(string characterName,string clipName)
        {
            var outer=Resources.Load<AnimationClip>("Characters/"+characterName+"/"+clipName);
            if(outer!=null)return outer;
            return Resources.Load<AnimationClip>("Characters/"+characterName+"/Animations/"+clipName);
        }
        Sprite Frame(Vessel vessel,string clip,int frame,bool eyes=false)
        {
            if(vessel==Vessel.LittleMoone)return eyes?null:MooneFrame(clip,frame);
            if(vessel==Vessel.Wanderer)return eyes?null:TravelFrame(clip,frame);
            string slug=Art[(int)vessel]+(eyes?"_eyes":"");if(!atlases.TryGetValue(slug,out var sprites))
            {
                Texture2D tex=Resources.Load<Texture2D>("Ember/Active/"+slug);if(tex==null)return eyes?null:Primitive("enemy");tex.filterMode=FilterMode.Point;int columns=tex.width/32;sprites=new Sprite[columns*4];
                for(int row=0;row<4;row++)for(int f=0;f<columns;f++)sprites[row*columns+f]=Sprite.Create(tex,new Rect(f*32,tex.height-(row+1)*32,32,32),new Vector2(.5f,1/32f),32);
                atlases[slug]=sprites;
            }
            int s=clip=="run"?1:clip=="cast"?2:clip=="hurt"?3:0,count=sprites.Length/4;return sprites[s*count+frame%count];
        }
        static readonly string[][] TravelFrames={
            new[]{"idle right 0 [cell_0_0]","idle right 1 [cell_0_1]","idle right 2 [cell_0_2]","idle right 3 [cell_0_3]","idle right 4 [cell_0_4]","idle right 5 [cell_0_5]","idle right 6 [cell_0_6]","idle right 7 [cell_0_7]"},
            new[]{"walk_right - Frame 0 [cell_1_0]","walk_right - Frame 1 [cell_1_1]","walk_right - Frame 2 [cell_1_2]","walk_right - Frame 3 [cell_1_3]","walk_right - Frame 4 [cell_1_4]","walk_right - Frame 5 [cell_1_5]","walk_right - Frame 6 [cell_1_6]","walk_right - Frame 7 [cell_1_7]"},
            new[]{"cast_right - Frame 0 [cell_2_0]","cast_right - Frame 1 [cell_2_1]","cast_right - Frame 2 [cell_2_2]","cast_right - Frame 3 [cell_2_3]","cast_right - Frame 4 [cell_2_4]","cast_right - Frame 5 [cell_2_5]","cast_right - Frame 6 [cell_2_6]","cast_right - Frame 7 [cell_2_7]"},
            new[]{"hurt_right - Frame 0 [cell_3_0]","hurt_right - Frame 1 [cell_3_1]","hurt_right - Frame 2 [cell_3_2]","hurt_right - Frame 3 [cell_3_3]","hurt_right - Frame 4 [cell_3_4]","hurt_right - Frame 5 [cell_3_5]","hurt_right - Frame 6 [cell_3_6]","hurt_right - Frame 7 [cell_3_7]"}
        };
        Dictionary<string,Sprite> travelSprites;
        Sprite TravelFrame(string clip,int frame)
        {
            if(travelSprites==null)
            {
                var loaded=Resources.LoadAll<Sprite>("Characters/Travel/hooded_traveler_big_hood_&_moone_boots___48x48");
                if(loaded!=null&&loaded.Length>0)
                {
                    travelSprites=loaded.ToDictionary(s=>s.name);
                }
                else travelSprites=new Dictionary<string,Sprite>();
            }
            int row=clip=="run"?1:clip=="cast"?2:clip=="hurt"?3:0;
            if(travelSprites!=null&&row<TravelFrames.Length)
            {
                int f=frame%8;
                if(travelSprites.TryGetValue(TravelFrames[row][f],out var sprite))return sprite;
            }
            return Primitive("enemy");
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
            touched.Add(key);
            if(!renderers.TryGetValue(key,out var r)||r==null||r.gameObject==null)
            {
                if(entityRoot==null)entityRoot=new GameObject("EntityRoot");
                r=new GameObject(key).AddComponent<SpriteRenderer>();
                r.transform.SetParent(entityRoot.transform);
                if(spriteMaterial!=null)r.sharedMaterial=spriteMaterial;
                else if(emissionMaterial!=null)r.sharedMaterial=emissionMaterial;
                renderers[key]=r;
            }
            r.enabled=true;r.sprite=sprite;r.color=color;r.flipX=flip;r.transform.position=new Vector3(pos.X,pos.Y,0);r.transform.localScale=Vector3.one*scale;r.sortingOrder=order??(5000-Mathf.RoundToInt(pos.Y*10));
            if(emissionMaterial!=null)
            {
                if(sprite==Primitive("diamond")||key.StartsWith("beacon_crystal")||key.StartsWith("sentinelRing")||key.StartsWith("richVeinSpark")||key.StartsWith("dropBeam")||key.StartsWith("dropAura")||key.StartsWith("swift_trail")||key.StartsWith("projectile")||key=="portal"||key=="mirage")
                {
                    r.sharedMaterial=emissionMaterial;
                }
            }
            return r;
        }
        void Render()
        {
            EnsureFogOfWar();
            touched.Clear();var local=Local;var focus=CameraTarget();V2 center=focus!=null?VisualPlayerPos(focus):new V2();
            if(fogRenderer!=null)
            {
                float fogPulse=.94f+.04f*Mathf.Sin(clock*1.6f);
                fogRenderer.color=new Color(1f,1f,1f,fogPulse);
            }
            int pixels=CameraZoomPixels[zoomLevel];
            cam.orthographicSize=Screen.height/(2f*pixels);
            cam.transform.position=new Vector3(center.X,center.Y,-10);
            float viewHalfH=cam.orthographicSize+2.4f;
            float viewHalfW=cam.orthographicSize*cam.aspect+2.4f;
            if(Mathf.Abs(center.X)<=viewHalfW+3f&&Mathf.Abs(center.Y)<=viewHalfH+3f)
            {
                bool isDefend=State.Phase==Phase.Eclipse||State.Phase==Phase.Extraction;
                float fillRatio=isDefend
                    ?(State.BeaconHP>0?Mathf.Clamp01(State.BeaconHP/600f):0f)
                    :(State.ChargeTarget>0?Mathf.Clamp01(State.Charge/(float)State.ChargeTarget):0f);
                bool hasEnergy=fillRatio>0.001f||State.Charge>0||State.BeaconHP>0;
                string lakeArt=hasEnergy?"beacon_lit":"beacon_dark";
                var lake=Put("beacon",Primitive(lakeArt),new V2(0,.35f),Color.white,1.45f,10);
                if(emissionMaterial!=null)lake.sharedMaterial=emissionMaterial;

                // Central Floating Monolith Crystal (Cách không lơ lửng trên mặt hồ)
                Color shadowCol=hasEnergy?new Color(.12f,.65f,.88f,.45f):new Color(0,0,0,.32f);
                var sr=Put("beacon_water_shadow",Primitive("circle"),new V2(0,.38f),shadowCol,hasEnergy?.85f:.65f,12);
                if(emissionMaterial!=null)sr.sharedMaterial=emissionMaterial;

                float floatY=Mathf.Sin(clock*2.0f)*.14f;
                Color crystalColor=hasEnergy?Color.white:new Color(.65f,.70f,.82f,.95f);
                float crystalY=1.05f+floatY; // Cách không: hovers gracefully above the lake

                // Base dark crystal monolith frame
                var coreDark=Put("beacon_crystal_dark_core",Primitive("beacon_crystal_dark"),new V2(0,crystalY),new Color(.6f,.65f,.75f,.9f),1.45f,1004);
                if(emissionMaterial!=null)coreDark.sharedMaterial=emissionMaterial;
                // Glowing cyan energy liquid rising/falling along Y-axis matching exact charge level
                if(hasEnergy&&fillRatio>0.001f)
                {
                    var coreLit=Put("beacon_crystal_core",Primitive("beacon_crystal_lit"),new V2(0,crystalY),crystalColor,1.45f,1005);
                    ApplyCrystalFill(coreLit,fillRatio,clock);
                    if(crystalFillMaterial==null||crystalFillMaterial.shader==null||!crystalFillMaterial.HasProperty("_FillLevel"))
                    {
                        coreLit.transform.localScale=new Vector3(1.45f,1.45f*fillRatio,1f);
                        coreLit.transform.position=new Vector3(0,crystalY-(1f-fillRatio)*.3f,0);
                    }

                    // Delicate rising energy particles floating upward along Y-axis from the liquid
                    for(int bi=0;bi<4;bi++)
                    {
                        float bSeed=bi*0.25f;
                        float bLife=Mathf.Repeat(clock*0.65f+bSeed,1f);
                        float bY=Mathf.Lerp(-0.25f,0.65f*fillRatio+0.1f,bLife);
                        float bX=Mathf.Sin(clock*3.2f+bi*2.1f)*0.24f;
                        float bAlpha=Mathf.Sin(bLife*Mathf.PI)*0.9f;
                        Color bCol=new Color(.4f,1f,.95f,bAlpha);
                        var bubble=Put($"crystal_bubble_{bi}",Primitive("circle"),new V2(bX,crystalY+bY),bCol,0.12f,1008);
                        if(emissionMaterial!=null)bubble.sharedMaterial=emissionMaterial;
                    }
                }

                // 3 Floating satellite shards orbiting around the monolith over the sacred water
                float orbSpeed=State.Charge>State.ChargeTarget?1.35f:.85f;
                for(int ci=0;ci<3;ci++)
                {
                    float angle=clock*orbSpeed+ci*(Mathf.PI*2f/3f);
                    float floatYOrb=Mathf.Sin(clock*2.4f+ci*2.1f)*.08f;
                    float rx=Mathf.Cos(angle)*.82f;
                    float depthY=Mathf.Sin(angle)*.35f;
                    float ry=crystalY+depthY+floatYOrb;
                    int sOrder=depthY<0?5006:5004;
                    var darkShard=Put("beacon_crystal_dark_"+ci,Primitive("beacon_crystal_dark"),new V2(rx,ry),new Color(.6f,.65f,.75f,.9f),.75f,sOrder-1);
                    if(emissionMaterial!=null)darkShard.sharedMaterial=emissionMaterial;
                    if(hasEnergy&&fillRatio>0.001f)
                    {
                        var crLit=Put("beacon_crystal_"+ci,Primitive("beacon_crystal_lit"),new V2(rx,ry),crystalColor,.75f,sOrder);
                        ApplyCrystalFill(crLit,fillRatio,clock);
                    }
                }
            }
            if(local!=null&&local.MirageLife>0)Put("mirage",Primitive("box"),local.MiragePos,Cyan*(.8f+.2f*Mathf.Sin(clock*3)),.7f);
            if(State.Phase==Phase.Extraction&&Mathf.Abs(center.X)<=viewHalfW+4f&&Mathf.Abs(center.Y)<=viewHalfH+4f)
                Put("portal",Primitive("ring"),new V2(),Cyan*(.8f+.2f*Mathf.Sin(clock*4)),4,5);
            var nodesArr=State.Nodes;
            V2 localVisual=local!=null?VisualPlayerPos(local):center;
            for(int ni=0;ni<nodesArr.Length;ni++)
            {
                var n=nodesArr[ni];
                if(Mathf.Abs(n.Pos.X-center.X)>viewHalfW||Mathf.Abs(n.Pos.Y-center.Y)>viewHalfH)continue;
                if(!IsExplored(n.Pos))continue;
                bool isWoodTree=!n.Dropped&&n.Resource==Resource.Wood;
                bool isMushroomNode=!n.Dropped&&((n.DropKind==2&&n.DropId==16)||n.DropId==16);
                bool isDustBush=!n.Dropped&&n.Resource==Resource.Dust&&!isMushroomNode;
                Color color=n.Resource==Resource.Core?Cyan:n.Resource==Resource.Umbra?new Color(.7f,.4f,1):n.Resource==Resource.Dust?Cyan:n.Resource==Resource.Wood?new Color(.5f,.35f,.2f):new Color(.6f,.65f,.8f);
                Sprite sprite=isWoodTree?Primitive("tree_oak"):isMushroomNode?Primitive("mushrooms"):isDustBush?Primitive("lumidust_bush"):n.Dropped?DroppedItemSprite(n):MaterialSprite(ResourceIcons[(int)n.Resource]);
                float scale=isWoodTree?1.45f:isMushroomNode?0.95f:isDustBush?1.35f:n.Dropped?.70f:.62f;
                V2 drawPos=n.Dropped?n.Pos+new V2(0,Mathf.Sin(clock*4.5f+n.Id)*.07f+.08f):n.Pos;
                if(isMushroomNode)
                {
                    float pulse=1f+Mathf.Sin(clock*3f+n.Id)*0.03f;
                    scale*=pulse;
                }
                else if(isDustBush)
                {
                    float sway=Mathf.Sin(clock*2.8f+n.Id*1.5f)*.06f;
                    float squish=1f+Mathf.Sin(clock*3.8f+n.Id*2f)*.08f;
                    drawPos=drawPos+new V2(sway*.08f,0);
                    scale*=squish;
                }
                int order=5000-Mathf.RoundToInt(n.Pos.Y*10);
                if(isMushroomNode)
                {
                    bool isNear=local!=null&&!local.Ghost&&V2.Distance(n.Pos,localVisual)<2.4f;
                    if(isNear)
                    {
                        float pulse=.78f+.22f*Mathf.Sin(clock*5f+n.Id);
                        Color mGlow=new Color(.82f,.48f,1f,.88f*pulse);
                        Outline(Put("nodeGlow"+n.Id,sprite,drawPos,mGlow,scale,order-1),sprite);
                    }
                    Put("node"+n.Id,sprite,drawPos,Color.white,scale,order);
                    for(int sIdx=0;sIdx<2;sIdx++)
                    {
                        float seed=n.Id*0.43f+sIdx*0.5f;
                        float life=Mathf.Repeat((clock*0.35f)+seed,1f);
                        float riseY=Mathf.Lerp(0.08f,0.85f,life);
                        float driftX=Mathf.Sin(clock*1.4f+sIdx*3.1f+n.Id)*0.12f;
                        float alpha=Mathf.Sin(life*Mathf.PI)*0.85f;
                        Color pColor=new Color(0.75f,0.45f,1f,alpha);
                        V2 partPos=n.Pos+new V2(driftX,riseY);
                        Put("shroom_spore_"+n.Id+"_"+sIdx,Primitive("circle"),partPos,pColor,0.12f,order+2);
                    }
                }
                else if(isDustBush)
                {
                    bool isNear=local!=null&&!local.Ghost&&V2.Distance(n.Pos,localVisual)<2.4f;
                    if(isNear)
                    {
                        float pulse=.78f+.22f*Mathf.Sin(clock*5f+n.Id);
                        Color dGlow=new Color(.35f,1f,.92f,.88f*pulse);
                        Outline(Put("nodeGlow"+n.Id,sprite,drawPos,dGlow,scale,order-1),sprite);
                    }
                    Put("node"+n.Id,sprite,drawPos,Color.white,scale,order);

                    // Delicate rising lumidust particles floating upward along Y axis
                    for(int pIdx=0;pIdx<4;pIdx++)
                    {
                        float seed=n.Id*0.37f+pIdx*0.25f;
                        float life=Mathf.Repeat((clock*0.42f)+seed,1f);
                        float riseY=Mathf.Lerp(0.12f,1.45f,life);
                        float driftX=Mathf.Sin(clock*1.6f+pIdx*2.4f+n.Id)*0.16f+(pIdx%2==0?0.06f:-0.06f);
                        float alpha=Mathf.Sin(life*Mathf.PI);
                        float sparkle=.8f+.25f*Mathf.Sin(clock*6f+pIdx*3.1f+n.Id);
                        float pScale=0.15f*sparkle*(.85f+.35f*(1f-life*.5f));
                        Color pColor=new Color(.55f,1f,.92f,alpha*.92f);
                        V2 partPos=n.Pos+new V2(driftX,riseY);
                        var pr=Put("dust_part_"+n.Id+"_"+pIdx,Primitive("circle"),partPos,pColor,pScale,order+2);
                        if(emissionMaterial!=null)pr.sharedMaterial=emissionMaterial;
                    }
                }
                else
                {
                    bool showGoldGlow=n.Dropped||(!isWoodTree&&(local!=null&&!local.Ghost&&V2.Distance(n.Pos,localVisual)<2.4f));
                    if(showGoldGlow)
                    {
                        float pulse=.82f+.18f*Mathf.Sin(clock*6f+n.Id);
                        Color glowColor=new Color(1f,.84f,.24f,pulse);
                        Outline(Put("nodeGlow"+n.Id,sprite,drawPos,glowColor,scale,order-1),sprite);
                    }
                    Put("node"+n.Id,sprite,drawPos,Color.white,scale,order);
                    if(n.Dropped)
                    {
                        float dPulse=.85f+.15f*Mathf.Sin(clock*4f+n.Id);
                        Color auraCol=n.Resource==Resource.Core?new Color(.35f,1f,.95f,.42f*dPulse):n.Resource==Resource.Umbra?new Color(.8f,.5f,1f,.42f*dPulse):new Color(.2f,1f,.9f,.32f*dPulse);
                        var dropAura=Put("dropAura"+n.Id,Primitive("circle"),n.Pos,auraCol,.95f,order-1);
                        if(emissionMaterial!=null)dropAura.sharedMaterial=emissionMaterial;
                        var dropBeam=Put("dropBeam"+n.Id,Primitive("diamond"),n.Pos+new V2(0,.38f+Mathf.Sin(clock*3f+n.Id)*.05f),auraCol,.48f,order+1);
                        if(emissionMaterial!=null)dropBeam.sharedMaterial=emissionMaterial;
                    }
                }
            }
            if(State.TreeRespawns!=null)
            {
                if(State.TreeRespawns.Length!=lastTreeRespawnCount)
                {
                    lastTreeRespawnCount=State.TreeRespawns.Length;
                    visibleChunk=new Vector2Int(int.MinValue,int.MinValue);
                }
                int size=State.Size;
                for(int tri=0;tri<State.TreeRespawns.Length;tri++)
                {
                    var tr=State.TreeRespawns[tri];
                    int key=tr.Ty*size+tr.Tx;
                    if(visibleTrees.TryGetValue(key,out var tSr))
                    {
                        if(tSr!=null&&tSr.gameObject!=null){tSr.enabled=false;treePool.Push(tSr);}
                        visibleTrees.Remove(key);
                        treeSpawnClock.Remove(key);
                        treeBaseScale.Remove(key);
                        treeBasePos.Remove(key);
                    }
                    if(currentTiles!=null&&key>=0&&key<currentTiles.Length&&currentTiles[key]!=0)
                    {
                        currentTiles[key]=0;
                    }
                    V2 stumpPos=new V2(tr.Tx-size/2f+.5f,tr.Ty-size/2f+.15f);
                    if(Mathf.Abs(stumpPos.X-center.X)>viewHalfW||Mathf.Abs(stumpPos.Y-center.Y)>viewHalfH)continue;
                    int sOrder=5000-Mathf.RoundToInt(stumpPos.Y*10);
                    if(tr.Tile==3)
                        Put("stump_"+tr.Tx+"_"+tr.Ty,Primitive("tree_stump"),stumpPos,Color.white,1.25f,sOrder);
                    else if(tr.Tile==2)
                        Put("rubble_"+tr.Tx+"_"+tr.Ty,Primitive("rock_small"),stumpPos,Color.white,0.85f,sOrder);
                }
            }
            else if(lastTreeRespawnCount!=0)
            {
                lastTreeRespawnCount=0;
                visibleChunk=new Vector2Int(int.MinValue,int.MinValue);
            }
            if(local!=null&&!local.Ghost&&currentTiles!=null)
            {
                int size=State.Size;
                int px=(int)Math.Floor(localVisual.X+size/2f),py=(int)Math.Floor(localVisual.Y+size/2f);
                int targetTx=-1,targetTy=-1;float closestD=2.6f;
                // 1. Scan grid around player to find closest target
                for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++)
                {
                    int tx=px+dx,ty=py+dy;
                    if(tx<0||ty<0||tx>=size||ty>=size)continue;
                    byte t=currentTiles[ty*size+tx];
                    if(t==3||t==2||t==7)
                    {
                        V2 blockPos=new V2(tx-size/2f+.5f,ty-size/2f+.5f);
                        float d=V2.Distance(localVisual,blockPos);
                        if(d<closestD){closestD=d;targetTx=tx;targetTy=ty;}
                    }
                }

                // 2. Render glowing outlines for ALL trees (tile 3) and rocks (tile 2) within range (3.4 tiles)
                for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++)
                {
                    int tx=px+dx,ty=py+dy;
                    if(tx<0||ty<0||tx>=size||ty>=size)continue;
                    byte t=currentTiles[ty*size+tx];
                    if(t!=3&&t!=2)continue;
                    V2 blockPos=new V2(tx-size/2f+.5f,ty-size/2f+.5f);
                    float d=V2.Distance(localVisual,blockPos);
                    if(d>3.4f)continue;

                    bool isTarget=(tx==targetTx&&ty==targetTy);
                    float pulse=isTarget?(.82f+.18f*Mathf.Sin(clock*7f)):.45f;
                    Color glowCol=t==3
                        ?(isTarget?new Color(1f,.92f,.35f,pulse):new Color(.95f,.82f,.38f,pulse*.75f))
                        :(isTarget?new Color(.38f,.92f,1f,pulse):new Color(.35f,.75f,.98f,pulse*.75f));

                    int key=ty*size+tx;
                    int baseTreeOrder=5000-Mathf.RoundToInt((ty-size/2f+.1f)*10);
                    if(t==3&&visibleTrees.TryGetValue(key,out var tSr)&&tSr!=null&&tSr.enabled&&tSr.sprite!=null)
                    {
                        var pos=tSr.transform.position;
                        var scale=tSr.transform.localScale.x;
                        var outlineR=Put("treeGlow_"+tx+"_"+ty,tSr.sprite,new V2(pos.x,pos.y),glowCol,scale,tSr.sortingOrder-1);
                        Outline(outlineR,tSr.sprite);
                    }
                    else if(t==2) // Rock/cliff
                    {
                        var def=Catalog.GetTileHarvestDef(tx,ty,2);
                        string rockSpriteName=def.Tier==0?"rock_small":def.Tier==1?"rock_boulder":"rock_large";
                        var rockSprite=Primitive(rockSpriteName);
                        V2 rPos=new V2(tx-size/2f+.5f,ty-size/2f+.2f);
                        float rScale=def.VisualScale;
                        Color rGlowCol=def.Tier==2?(isTarget?new Color(.35f,1f,.95f,pulse):new Color(.35f,1f,.95f,pulse*.8f)):glowCol;
                        var outlineR=Put("rockGlow_"+tx+"_"+ty,rockSprite,rPos,rGlowCol,rScale,baseTreeOrder-1);
                        Outline(outlineR,rockSprite);
                        Put("rockBody_"+tx+"_"+ty,rockSprite,rPos,Color.white,rScale,baseTreeOrder);
                        if(def.Tier==2) // Rich Vein crystal sparkle
                        {
                            float cPulse=.8f+.2f*Mathf.Sin(clock*5f+tx*3f);
                            var coreSpark=Put("richVeinSpark_"+tx+"_"+ty,Primitive("diamond"),rPos+new V2(0,.25f),new Color(.4f,1f,.95f,cPulse),.42f,baseTreeOrder+1);
                            if(emissionMaterial!=null)coreSpark.sharedMaterial=emissionMaterial;
                        }
                    }

                    if(isTarget)
                    {
                        // Target ground rune indicator - strictly placed on ground layer UNDER the tree/rock (order <= 20)
                        V2 groundPos=new V2(tx-size/2f+.5f,ty-size/2f+.12f);
                        int groundOrder=Mathf.Min(baseTreeOrder-3,20);
                        var ringR=Put("blockTargetGlow",Primitive("ring"),groundPos,glowCol,1.18f,groundOrder);
                        if(emissionMaterial!=null)ringR.sharedMaterial=emissionMaterial;
                        var innerR=Put("blockTargetGlowInner",Primitive("circle"),groundPos,new Color(glowCol.r,glowCol.g,glowCol.b,.22f*pulse),1.25f,groundOrder-1);
                        if(emissionMaterial!=null)innerR.sharedMaterial=emissionMaterial;
                    }
                }
            }
            var structsArr=State.Structures;
            for(int si=0;si<structsArr.Length;si++)
            {
                var s=structsArr[si];
                if(Mathf.Abs(s.Pos.X-center.X)>viewHalfW||Mathf.Abs(s.Pos.Y-center.Y)>viewHalfH)continue;
                if(!IsExplored(s.Pos)&&s.Kind!=1)continue;
                bool isMerchant=s.Kind==5||s.Kind==10;
                string structArt=s.Kind==0?"torch_ground":s.Kind==1?"flare":s.Kind==2?"wall":s.Kind==3?"trap":s.Kind==4?"chest":isMerchant?"merchant":s.Kind==11?"chest":s.Kind==12?"abandoned_chest":s.Kind==13?"abandoned_chest_open":"diamond";
                float structScale=s.Kind==2?1.4f:s.Kind==4?1.35f:isMerchant?1.25f:s.Kind==11?1.4f:(s.Kind==12||s.Kind==13)?1.35f:s.Kind==0?1.2f:1f;
                int mFrame=(int)(clock*3.5f+s.Id)%4;
                Sprite sSprite=isMerchant?EnemyFrame("merchant",mFrame):Primitive(structArt);
                int sOrder=5000-Mathf.RoundToInt(s.Pos.Y*10);
                Put("structure"+s.Id,sSprite,s.Pos,Color.white,structScale,sOrder);
                if(s.Kind==0) // Ground torch: animated flickering flame
                {
                    int flameFrame=(int)(clock*8f+s.Id)%8;
                    Sprite flameSprite=EnemyFrame("torch_fire",flameFrame);
                    var flameR=Put("torchFlame"+s.Id,flameSprite,s.Pos+new V2(0,.08f),Color.white,structScale,sOrder+1);
                    if(emissionMaterial!=null)flameR.sharedMaterial=emissionMaterial;

                    float fPulse=.82f+.18f*Mathf.Sin(clock*10f+s.Id*2.3f);
                    Color flameGlow=new Color(1f,.65f,.2f,.35f*fPulse);
                    var halo=Put("torchHalo"+s.Id,Primitive("circle"),s.Pos+new V2(0,.85f),flameGlow,1.15f,sOrder-1);
                    if(emissionMaterial!=null)halo.sharedMaterial=emissionMaterial;
                }
                if(s.Kind==12)
                {
                    float pulse=.8f+.2f*Mathf.Sin(clock*3.2f+s.Id);
                    Color cyanGlow=new Color(.47f,.84f,.91f,.32f*pulse);
                    var chestHalo=Put("chestGlow"+s.Id,Primitive("circle"),s.Pos+new V2(0,.25f),cyanGlow,.85f,-1);
                    if(emissionMaterial!=null)chestHalo.sharedMaterial=emissionMaterial;
                }
                else if(s.Kind==13)
                {
                    float fade=Mathf.Clamp01(s.Life/30f);
                    float pulse=.7f+.3f*Mathf.Sin(clock*4.5f+s.Id);
                    Color mistGlow=new Color(.55f,.4f,.9f,.28f*fade*pulse);
                    var openHalo=Put("chestGlow"+s.Id,Primitive("circle"),s.Pos+new V2(0,.35f),mistGlow,1.1f,-1);
                    if(emissionMaterial!=null)openHalo.sharedMaterial=emissionMaterial;
                }
                if(isMerchant)
                {
                    float mPulse=.85f+.15f*Mathf.Sin(clock*4f+s.Id);
                    Color mGlow=new Color(1f,.82f,.35f,.45f*mPulse);
                    var lanternHalo=Put("merchantGlow"+s.Id,Primitive("circle"),s.Pos+new V2(-0.55f,1.15f),mGlow,1.1f,-1);
                    if(emissionMaterial!=null)lanternHalo.sharedMaterial=emissionMaterial;
                }
            }
            var enemiesArr=State.Enemies;
            for(int ei=0;ei<enemiesArr.Length;ei++)
            {
                var e=enemiesArr[ei];
                V2 ePos=VisualEnemyPos(e);
                if(Mathf.Abs(ePos.X-center.X)>viewHalfW+1.5f||Mathf.Abs(ePos.Y-center.Y)>viewHalfH+1.5f)continue;
                if(!IsExplored(ePos)&&!e.IsWorldBoss&&e.Kind!=EnemyKind.Boss)continue;
                string art=e.Kind==EnemyKind.Boss?(e.BossType==BossType.CinderBehemoth?"boss_cinder":e.BossType==BossType.EclipseWeaver?"boss_eclipse":"boss"):e.Kind==EnemyKind.Nest?"nest":e.Kind==EnemyKind.SwampLeech?"enemy_swamp":e.Kind==EnemyKind.VoidStalker?"enemy_void":e.Elite||e.Kind==EnemyKind.Sentinel?"enemy_sentinel":"enemy_shade";
                float scale=e.IsWorldBoss?Catalog.BossFor(e.BossType).Scale:e.Kind==EnemyKind.Boss?1.5f:e.Elite||e.Kind==EnemyKind.Sentinel?1.4f:1;
                Color glowColor=e.IsWorldBoss?BossAccentColor(e.BossType):e.Kind==EnemyKind.Sentinel?new Color(.35f,.92f,1f):Coral;
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
                float bob=e.Kind==EnemyKind.Nest?0f:e.Kind==EnemyKind.Sentinel?Mathf.Sin(clock*4.2f+e.Id)*.08f:moving?Mathf.Sin(clock*13f+e.Id)*.035f:Mathf.Sin(clock*3.2f+e.Id)*.018f;
                V2 renderPos=ePos+new V2(0,bob);
                Outline(Put("enemyGlow"+e.Id,artSprite,renderPos,glowColor,scale,5000-Mathf.RoundToInt(ePos.Y*10)-1,flip),artSprite);
                Put("enemy"+e.Id,artSprite,renderPos,bodyTint,scale,flip:flip);
                if(e.IsWorldBoss&&e.ChargeTime>0)
                {
                    V2 cDir=e.ChargeDir.Length>0.01f?e.ChargeDir.Normal:new V2(flip?1f:-1f,0f);
                    for(int ghost=1;ghost<=3;ghost++)
                    {
                        float gDist=ghost*0.35f;
                        float gAlpha=0.55f*(1f-ghost*0.25f);
                        V2 gPos=renderPos-cDir*gDist;
                        Color gColor=new Color(glowColor.r,glowColor.g,glowColor.b,gAlpha);
                        var gSr=Put("bossGhost"+e.Id+"_"+ghost,artSprite,gPos,gColor,scale,5000-Mathf.RoundToInt(ePos.Y*10)-2-ghost,flip);
                        if(emissionMaterial!=null)gSr.sharedMaterial=emissionMaterial;
                    }
                }
                if(e.Kind==EnemyKind.Sentinel)
                {
                    float rot=clock*140f+e.Id*45f;
                    for(int o=0;o<3;o++)
                    {
                        float a=(rot+o*120f)*Mathf.Deg2Rad;
                        V2 ringOff=new V2(Mathf.Cos(a)*.52f,Mathf.Sin(a)*.26f);
                        var shard=Put("sentinelRing"+e.Id+"_"+o,Primitive("diamond"),renderPos+ringOff,new Color(.45f,1f,.95f,.9f),.44f,5002-Mathf.RoundToInt(ePos.Y*10));
                        if(emissionMaterial!=null)shard.sharedMaterial=emissionMaterial;
                    }
                }
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
                int frames=p.Vessel==Vessel.LittleMoone||p.Vessel==Vessel.Wanderer||Art[(int)p.Vessel]=="lyra_moonscout"?8:4;
                int raw=(int)((clock-anim.began)*((p.Vessel==Vessel.LittleMoone||p.Vessel==Vessel.Wanderer)?6:clip=="run"?10:clip=="cast"?(frames==8?12:9):clip=="hurt"?(frames==8?10:8):6));int f=clip=="cast"||clip=="hurt"?Mathf.Min(frames-1,raw):raw%frames;
                var sprite=hallucination?Primitive("enemy_void"):Frame(p.Vessel,clip,f);Color tint=Color.white;
                Put("shadow"+p.Id,Primitive("circle"),pPos,new Color(0,0,0,.35f),.8f,5);
                Outline(Put("outline"+p.Id,sprite,pPos,glow,1,5000-Mathf.RoundToInt(pPos.Y*10)-1,!p.Right),sprite);
                var body=Put("player"+p.Id,sprite,pPos,tint,1,flip:!p.Right);
                if(!hallucination){var eyes=Frame(p.Vessel,clip,f,true);if(eyes!=null){var glowEyes=Put("eyes"+p.Id,eyes,pPos,tint,1,5001-Mathf.RoundToInt(pPos.Y*10),!p.Right);if(emissionMaterial!=null)glowEyes.sharedMaterial=emissionMaterial;}}
                if(!hallucination&&p.CastTime<=0)DrawHands(p,body,pPos);
                if(p.Hat>=0&&!hallucination)Put("hat"+p.Id,Primitive(new[]{"hat_straw","hat_witch","hat_candle","hat_jester"}[p.Hat]),pPos+new V2(0,p.Hat==3?.47f:.82f),Color.white,p.Hat==3?.7f:.8f,5010-Mathf.RoundToInt(pPos.Y*10));
                if(p.Pet>=0&&!hallucination)Put("pet"+p.Id,Primitive(new[]{"pet_firefly","pet_bat","pet_cat","pet_ghost"}[p.Pet]),pPos+new V2(-1,.1f+Mathf.Sin(clock*4)*.15f),Color.white,.65f);
                if(p.ShieldTime>0)Put("shield"+p.Id,Primitive("ring"),pPos+new V2(0,.5f),Cyan,2,-1);
                if(p.HasBuff(BuffKind.Swiftness))
                {
                    Put("swift_aura"+p.Id,Primitive("ring"),pPos+new V2(0,.45f),new Color(.28f,.85f,.95f,.40f+Mathf.Sin(clock*8f)*.18f),1.4f,-1);
                    if(p.Move.Length>.1f)
                    {
                        for(int wi=0;wi<3;wi++)
                        {
                            float wOffset=(wi+1)*0.22f;
                            V2 streakPos=pPos-p.Move*wOffset+new V2(0,Mathf.Sin(clock*14f+wi)*0.10f);
                            float alpha=(1f-wi*0.3f)*0.40f;
                            var st=Put($"swift_trail_{p.Id}_{wi}",Primitive("diamond"),streakPos,new Color(.4f,.95f,.8f,alpha),.25f-wi*.05f);
                            if(emissionMaterial!=null)st.sharedMaterial=emissionMaterial;
                        }
                    }
                }
                if(p.HasBuff(BuffKind.IronSkin))
                {
                    Put("ironskin_aura"+p.Id,Primitive("ring"),pPos+new V2(0,.5f),new Color(.95f,.8f,.25f,.50f),1.6f,-1);
                }
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
                float pAngle=Mathf.Atan2(shot.Velocity.Y,shot.Velocity.X)*Mathf.Rad2Deg;
                if(shot.Weapon==Weapon.Crossbow)
                {
                    var pr=Put("projectile"+shot.Id,Primitive("projectile_arrow"),sPos+new V2(0,.22f),Color.white,.85f,1500);
                    pr.transform.rotation=Quaternion.Euler(0,0,pAngle);
                }
                else if(shot.Weapon==Weapon.CrystalAxe)
                {
                    // Class 2: Kiếm Khí (Blade Wave)
                    float wavePulse=1f+0.12f*Mathf.Sin(clock*15f+shot.Id);
                    var pr=Put("projectile"+shot.Id,Primitive("blade_wave"),sPos+new V2(0,.22f),Color.white,.95f*wavePulse,1510);
                    pr.transform.rotation=Quaternion.Euler(0,0,pAngle);
                    if(emissionMaterial!=null)pr.sharedMaterial=emissionMaterial;

                    // Starlight cyan trailing glow
                    float trailAlpha=0.6f+0.3f*Mathf.Sin(clock*10f);
                    var halo=Put("bladeWaveGlow"+shot.Id,Primitive("circle"),sPos+new V2(0,.22f),new Color(0.2f,0.9f,1f,trailAlpha*0.35f),1.1f,1505);
                    if(emissionMaterial!=null)halo.sharedMaterial=emissionMaterial;
                }
                else if(shot.Weapon==Weapon.LumenGun)
                {
                    // Class 3: Plasma energy beam
                    var pr=Put("projectile"+shot.Id,Primitive("projectile_plasma"),sPos+new V2(0,.25f),Color.white,0.85f,1520);
                    pr.transform.rotation=Quaternion.Euler(0,0,pAngle);
                    if(emissionMaterial!=null)pr.sharedMaterial=emissionMaterial;

                    var glow=Put("lumenGlow"+shot.Id,Primitive("circle"),sPos+new V2(0,.25f),new Color(0.4f,1f,0.95f,0.5f),0.75f,1515);
                    if(emissionMaterial!=null)glow.sharedMaterial=emissionMaterial;
                }
                else if(shot.Weapon==Weapon.ShadowBlades)
                {
                    // Song Trảm Chữ X Tím Hư Không (Cross-Scissor Void Slash)
                    float slashPulse=1f+0.12f*Mathf.Sin(clock*20f+shot.Id);
                    var pr=Put("projectile"+shot.Id,Primitive("shadow_cross_slash"),sPos+new V2(0,.22f),Color.white,.95f*slashPulse,1510);
                    pr.transform.rotation=Quaternion.Euler(0,0,pAngle);
                    if(emissionMaterial!=null)pr.sharedMaterial=emissionMaterial;

                    // Deep purple void halo
                    float trailAlpha=0.6f+0.3f*Mathf.Sin(clock*12f);
                    var halo=Put("shadowSlashGlow"+shot.Id,Primitive("circle"),sPos+new V2(0,.22f),new Color(0.7f,0.2f,1f,trailAlpha*0.35f),1.1f,1505);
                    if(emissionMaterial!=null)halo.sharedMaterial=emissionMaterial;
                }
                else
                {
                    var pr=Put("projectile"+shot.Id,Primitive("diamond"),sPos+new V2(0,.3f),new Color(.78f,.98f,1f),.30f);
                    pr.transform.rotation=Quaternion.Euler(0,0,pAngle);
                    if(emissionMaterial!=null)pr.sharedMaterial=emissionMaterial;
                }
            }
            if(local!=null&&local.Dread>=30&&!local.Safe)
            {
                float cycle=Mathf.Sin(clock*.75f);
                if(cycle>0.3f)
                {
                    float fade=Mathf.Clamp01((cycle-0.3f)/0.7f);
                    float sign=Mathf.Sin(clock*.25f)>0?1:-1;
                    var pos=localVisual+new V2(sign*(cam.orthographicSize*cam.aspect-0.8f),Mathf.Sin(clock*1.1f)*1.6f);
                    int frame=(int)(clock*4)%4;
                    var shadowSprite=EnemyFrame("enemy_void",frame);
                    Color shadowCol=new Color(0.10f,0.06f,0.18f,fade*0.50f);
                    Put("falseShadow",shadowSprite,pos,shadowCol,1.2f,2000,flip:sign>0);
                }
            }
            foreach(var pair in renderers)if(!touched.Contains(pair.Key)&&pair.Value!=null&&pair.Value.gameObject!=null&&pair.Value.enabled)pair.Value.enabled=false;
            // Reclaim transient node/enemy renderers instead of retaining an entire run.
            if(renderers.Count>1400)foreach(var key in renderers.Where(p=>p.Value==null||p.Value.gameObject==null||!p.Value.enabled).Select(p=>p.Key).ToArray()){if(renderers.TryGetValue(key,out var sr)&&sr!=null&&sr.gameObject!=null)Destroy(sr.gameObject);renderers.Remove(key);}
        }
        static void Blit16(Color32[] dest,int px,int tx,int ty,Color32[] src,int srcW,int sc,int sr,bool blend=false)
        {
            if(src==null||srcW<=0)return;
            int srcH=src.Length/srcW;
            int sy0=srcH-(sr+1)*16;
            int sx0=sc*16;
            for(int ly=0;ly<16;ly++)
            {
                int dy=ty*16+ly;
                int sy=sy0+ly;
                if(sy<0||sy>=srcH||dy<0||dy>=px)continue;
                for(int lx=0;lx<16;lx++)
                {
                    int dx=tx*16+lx;
                    int sx=sx0+lx;
                    if(sx<0||sx>=srcW||dx<0||dx>=px)continue;
                    Color32 s=src[sy*srcW+sx];
                    if(blend)
                    {
                        if(s.a==0)continue;
                        if(s.a==255)dest[dy*px+dx]=s;
                        else
                        {
                            Color32 d=dest[dy*px+dx];
                            float a=s.a/255f;
                            dest[dy*px+dx]=new Color32((byte)(s.r*a+d.r*(1f-a)),(byte)(s.g*a+d.g*(1f-a)),(byte)(s.b*a+d.b*(1f-a)),255);
                        }
                    }
                    else dest[dy*px+dx]=s;
                }
            }
        }
        Texture2D mapTexture;
        Color32[] mapColors;
        Color32[] gPix, pPix, wPix, bPix, hPix;
        int gW, pW, wW, bW, hW;

        public void UpdateMapTileVisual(int tx,int ty,byte tile)
        {
            if(mapColors==null||currentTiles==null)return;
            int size=State.Size;
            int px=size*16;
            if(tx<0||tx>=size||ty<0||ty>=size)return;
            int dx=tx-size/2,dy=ty-size/2;
            int dist2=dx*dx+dy*dy;
            bool isSanctuary=dist2<=38;
            int h=((tx*374761393)^(ty*668265263))&0x7FFFFFFF;

            if(tile==4||tx<3||ty<3||tx>=size-3||ty>=size-3)
            {
                Color32 c=new Color32(4,3,7,255);
                for(int ly=0;ly<16;ly++)for(int lx=0;lx<16;lx++)mapColors[(ty*16+ly)*px+(tx*16+lx)]=c;
            }
            else if(tile==1) // Water
            {
                if(wPix!=null)Blit16(mapColors,px,tx,ty,wPix,wW,h%4,0);
                else
                {
                    Color32 c=new Color32(45,120,155,255);
                    for(int ly=0;ly<16;ly++)for(int lx=0;lx<16;lx++)mapColors[(ty*16+ly)*px+(tx*16+lx)]=c;
                }
            }
            else if(tile==2) // Highland / Cliff
            {
                if(hPix!=null)Blit16(mapColors,px,tx,ty,hPix,hW,1,2);
                else
                {
                    Color32 c=new Color32(110,80,130,255);
                    for(int ly=0;ly<16;ly++)for(int lx=0;lx<16;lx++)mapColors[(ty*16+ly)*px+(tx*16+lx)]=c;
                }
            }
            else if(tile==5) // Wooden Bridge
            {
                if(wPix!=null)Blit16(mapColors,px,tx,ty,wPix,wW,h%4,0); // water base
                if(bPix!=null)
                {
                    bool bNorth=(ty+1<size)&&currentTiles[(ty+1)*size+tx]==5;
                    bool bSouth=(ty-1>=0)&&currentTiles[(ty-1)*size+tx]==5;
                    bool bWest=(tx-1>=0)&&currentTiles[ty*size+(tx-1)]==5;
                    bool bEast=(tx+1<size)&&currentTiles[ty*size+(tx+1)]==5;

                    bool landNorth=(ty+1<size)&&(currentTiles[(ty+1)*size+tx]==0||currentTiles[(ty+1)*size+tx]==6);
                    bool landSouth=(ty-1>=0)&&(currentTiles[(ty-1)*size+tx]==0||currentTiles[(ty-1)*size+tx]==6);
                    bool landWest=(tx-1>=0)&&(currentTiles[ty*size+(tx-1)]==0||currentTiles[ty*size+(tx-1)]==6);
                    bool landEast=(tx+1<size)&&(currentTiles[ty*size+(tx+1)]==0||currentTiles[ty*size+(tx+1)]==6);

                    bool isVert=(bNorth||bSouth)||(landNorth&&landSouth)||(!bWest&&!bEast&&(landNorth||landSouth));
                    if(!isVert&&!bWest&&!bEast&&!landWest&&!landEast)
                        isVert=Math.Abs(dy)>=Math.Abs(dx);

                    if(isVert)
                    {
                        int col=h%2==0?0:1;
                        if(!bNorth)Blit16(mapColors,px,tx,ty,bPix,bW,col,0,blend:true);
                        else if(!bSouth)Blit16(mapColors,px,tx,ty,bPix,bW,col,2,blend:true);
                        else Blit16(mapColors,px,tx,ty,bPix,bW,col,1,blend:true);
                    }
                    else
                    {
                        int row=h%2==0?0:1;
                        if(!bWest)Blit16(mapColors,px,tx,ty,bPix,bW,2,row,blend:true);
                        else if(!bEast)Blit16(mapColors,px,tx,ty,bPix,bW,4,row,blend:true);
                        else Blit16(mapColors,px,tx,ty,bPix,bW,3,row,blend:true);
                    }
                }
            }
            else if(tile==6) // Dirt Path
            {
                if(gPix!=null)Blit16(mapColors,px,tx,ty,gPix,gW,1,1); // grass base
                if(pPix!=null)Blit16(mapColors,px,tx,ty,pPix,pW,(h%2==0?0:2),1,blend:true);
            }
            else // Grass (tile 0 or tree obstacle 3)
            {
                if(gPix!=null)
                {
                    int m=h%100;
                    if(m<14)Blit16(mapColors,px,tx,ty,gPix,gW,h%6,5);
                    else if(m<26)Blit16(mapColors,px,tx,ty,gPix,gW,h%6,6);
                    else Blit16(mapColors,px,tx,ty,gPix,gW,1,1);
                }
                else
                {
                    Color32 c=new Color32(192,212,112,255);
                    for(int ly=0;ly<16;ly++)for(int lx=0;lx<16;lx++)mapColors[(ty*16+ly)*px+(tx*16+lx)]=c;
                }
            }
        }

        void BuildMap()
        {
            if(State==null)return;
            renderedSeed=State.Seed;
            if(mapRoot==null)
            {
                mapRoot=GameObject.Find("Ember map");
                if(mapRoot==null)mapRoot=new GameObject("Ember map");
            }
            if(white==null)
            {
                white=new Texture2D(1,1);
                white.SetPixel(0,0,Color.white);
                white.Apply();
            }
            foreach(Transform t in mapRoot.transform)Destroy(t.gameObject);
            visibleTrees.Clear();treePool.Clear();visibleDecor.Clear();decorPool.Clear();keepDecor.Clear();dropDecor.Clear();treeSpawnClock.Clear();treeBaseScale.Clear();treeBasePos.Clear();visibleChunk=new Vector2Int(int.MinValue,int.MinValue);
            float edgeWorld=State.Size/2f-3;
            var fog=new GameObject("Abyss / unlit impenetrable fog").AddComponent<SpriteRenderer>();fog.transform.SetParent(mapRoot.transform);fog.sprite=Sprite.Create(white,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);fog.transform.localScale=Vector3.one*(State.Size+40);fog.sortingOrder=2;
            var fogMaterial=Resources.Load<Material>("Ember/EmberAbyss");if(fogMaterial!=null){fog.material=new Material(fogMaterial);fog.material.SetFloat("_Boundary",edgeWorld);}
            foreach(var side in new[]{new Vector3(-edgeWorld-2,0),new Vector3(edgeWorld+2,0),new Vector3(0,-edgeWorld-2),new Vector3(0,edgeWorld+2)})
            {
                var border=new GameObject("Abyss / physical boundary");border.transform.SetParent(mapRoot.transform);border.transform.position=side;
                var collider=border.AddComponent<BoxCollider2D>();collider.size=side.x!=0?new Vector2(4,State.Size+8):new Vector2(State.Size+8,4);
            }
            byte[] tiles=(host&&World!=null&&World.Tiles!=null)?World.Tiles:EmberWorld.MakeTiles(State.Seed,State.Size);currentTiles=tiles;int size=State.Size;
            exploredSize=size;
            exploredTiles=new bool[size*size];
            int initR=14;
            int cx=size/2,cy=size/2;
            for(int dy=-initR;dy<=initR;dy++)
            {
                for(int dx=-initR;dx<=initR;dx++)
                {
                    if(dx*dx+dy*dy<=initR*initR)
                    {
                        int tx=cx+dx,ty=cy+dy;
                        if(tx>=0&&tx<size&&ty>=0&&ty<size)exploredTiles[ty*size+tx]=true;
                    }
                }
            }
            if(minimapTex==null||minimapTex.width!=size||minimapTex.height!=size)
            {
                minimapTex=new Texture2D(size,size,TextureFormat.RGBA32,false);
                minimapTex.filterMode=FilterMode.Point;
                minimapTex.wrapMode=TextureWrapMode.Clamp;
            }
            minimapColors=new Color32[size*size];
            minimapDirty=true;
            UpdateMinimapTexture();
            EnsureFogOfWar();
            int px=size*16;
            mapTexture=new Texture2D(px,px,TextureFormat.RGBA32,false);
            mapTexture.filterMode=FilterMode.Point;
            mapColors=new Color32[px*px];

            // Load authentic 16x16 Sprout Lands tilesets
            var grassTex=Resources.Load<Texture2D>("Ember/Tilesets/grass_tileset");
            var pathTex=Resources.Load<Texture2D>("Ember/Tilesets/paths_tileset");
            var waterTex=Resources.Load<Texture2D>("Ember/Tilesets/water_tileset");
            var bridgeTex=Resources.Load<Texture2D>("Ember/Tilesets/bridge_tileset");
            var hillsTex=Resources.Load<Texture2D>("Ember/Tilesets/hills_tileset");

            gPix=null;pPix=null;wPix=null;bPix=null;hPix=null;
            gW=0;pW=0;wW=0;bW=0;hW=0;
            try{if(grassTex!=null){gPix=grassTex.GetPixels32();gW=grassTex.width;}}catch{}
            try{if(pathTex!=null){pPix=pathTex.GetPixels32();pW=pathTex.width;}}catch{}
            try{if(waterTex!=null){wPix=waterTex.GetPixels32();wW=waterTex.width;}}catch{}
            try{if(bridgeTex!=null){bPix=bridgeTex.GetPixels32();bW=bridgeTex.width;}}catch{}
            try{if(hillsTex!=null){hPix=hillsTex.GetPixels32();hW=hillsTex.width;}}catch{}

            for(int ty=0;ty<size;ty++)for(int tx=0;tx<size;tx++)
            {
                byte tile=tiles[ty*size+tx];
                UpdateMapTileVisual(tx,ty,tile);
            }
            mapTexture.SetPixels32(mapColors);
            mapTexture.Apply();
            var floor=new GameObject("Three biomes").AddComponent<SpriteRenderer>();
            floor.transform.SetParent(mapRoot.transform);
            if(spriteMaterial!=null)floor.sharedMaterial=spriteMaterial;
            floor.sprite=Sprite.Create(mapTexture,new Rect(0,0,px,px),new Vector2(.5f,.5f),16);
            floor.sortingOrder=0;
        }
        readonly HashSet<int> keepTrees=new HashSet<int>();
        readonly List<int> dropTrees=new List<int>();
        readonly Dictionary<int,float> treeSpawnClock=new Dictionary<int,float>();
        readonly Dictionary<int,float> treeBaseScale=new Dictionary<int,float>();
        readonly Dictionary<int,Vector3> treeBasePos=new Dictionary<int,Vector3>();

        void AnimateEmergingTrees()
        {
            if(treeSpawnClock.Count==0)return;
            List<int> done=null;
            foreach(var kv in treeSpawnClock)
            {
                if(!visibleTrees.TryGetValue(kv.Key,out var tSr))
                {
                    if(done==null)done=new List<int>();
                    done.Add(kv.Key);
                    continue;
                }
                float age=clock-kv.Value;
                const float dur=0.65f;
                if(age>=dur)
                {
                    if(treeBaseScale.TryGetValue(kv.Key,out var bs))tSr.transform.localScale=Vector3.one*bs;
                    if(treeBasePos.TryGetValue(kv.Key,out var bp))tSr.transform.position=bp;
                    tSr.color=Color.white;
                    if(done==null)done=new List<int>();
                    done.Add(kv.Key);
                }
                else
                {
                    float t=Mathf.Clamp01(age/dur);
                    float ease=Mathf.SmoothStep(0f,1f,t);
                    if(treeBaseScale.TryGetValue(kv.Key,out var bs))tSr.transform.localScale=Vector3.one*Mathf.Lerp(bs*0.15f,bs,ease);
                    if(treeBasePos.TryGetValue(kv.Key,out var bp))tSr.transform.position=bp+new Vector3(0,Mathf.Lerp(-0.4f,0f,ease),0);
                    tSr.color=new Color(1f,1f,1f,ease);
                }
            }
            if(done!=null)
            {
                for(int i=0;i<done.Count;i++)
                {
                    treeSpawnClock.Remove(done[i]);
                    treeBaseScale.Remove(done[i]);
                    treeBasePos.Remove(done[i]);
                }
            }
        }

        void StreamTrees()
        {
            AnimateEmergingTrees();
            if(currentTiles==null||State==null)return;var pos=CameraTarget()?.Pos??new V2();int size=State.Size;
            var chunk=new Vector2Int(Mathf.FloorToInt(pos.X/4),Mathf.FloorToInt(pos.Y/4));if(chunk==visibleChunk)return;visibleChunk=chunk;
            int halfX=Mathf.CeilToInt(cam.orthographicSize*cam.aspect)+6;
            int halfY=Mathf.CeilToInt(cam.orthographicSize)+6;
            int cx=chunk.x*4+size/2,cy=chunk.y*4+size/2;
            keepTrees.Clear();dropTrees.Clear();
            keepDecor.Clear();dropDecor.Clear();
            int yMin=Mathf.Max(3,cy-halfY),yMax=Mathf.Min(size-3,cy+halfY+2);
            int xMin=Mathf.Max(3,cx-halfX),xMax=Mathf.Min(size-3,cx+halfX+2);
            for(int y=yMin;y<yMax;y++)for(int x=xMin;x<xMax;x++)
            {
                int idx=y*size+x;
                if(idx<0||idx>=currentTiles.Length)continue;
                if(currentTiles[idx]==3)
                {
                    int key=idx;keepTrees.Add(key);if(visibleTrees.ContainsKey(key))continue;
                float dx=x-size/2f,dy=y-size/2f;
                float r=Mathf.Sqrt(dx*dx+dy*dy);
                int hash=((x*374761393)^(y*668265263))&0x7FFFFFFF;
                string treeArt;
                if(r>=size*.37f)
                {
                    int m=hash%6;
                    treeArt=(m<=2)?"tree_void":(m<=4)?"tree_dead":"tree_snow";
                }
                else if(r>=size*.22f)
                {
                    int m=hash%6;
                    treeArt=(m<=2)?"tree_pine":(m<=4)?"tree_dead":"tree_palm";
                }
                else
                {
                    int m=hash%10;
                    treeArt=(m<=2)?"tree_oak":(m<=5)?"tree_apple":(m<=7)?"tree_autumn":"tree_pine";
                }
                SpriteRenderer tree=null;
                while(treePool.Count>0&&(tree==null||tree.gameObject==null))tree=treePool.Pop();
                if(tree!=null&&tree.gameObject!=null)tree.enabled=true;
                else
                {
                    tree=new GameObject("Ancient tree").AddComponent<SpriteRenderer>();
                    if(mapRoot!=null)tree.transform.SetParent(mapRoot.transform);
                    if(spriteMaterial!=null)tree.sharedMaterial=spriteMaterial;
                }
                tree.sprite=Primitive(treeArt);
                var treeDef=Catalog.GetTileHarvestDef(x,y,3);
                float scale=treeDef.VisualScale;
                Vector3 basePos=new Vector3(x-size/2+.5f,y-size/2+.1f,0);
                bool outsideSanctuary=r>14f;
                if(outsideSanctuary&&!treeSpawnClock.ContainsKey(key))
                {
                    treeSpawnClock[key]=clock;
                    treeBaseScale[key]=scale;
                    treeBasePos[key]=basePos;
                    tree.color=new Color(1f,1f,1f,0f);
                    tree.transform.localScale=Vector3.one*(scale*0.15f);
                    tree.transform.position=basePos+new Vector3(0,-0.4f,0);
                }
                else
                {
                    tree.color=Color.white;
                    tree.transform.localScale=Vector3.one*scale;
                    tree.transform.position=basePos;
                }
                tree.sortingOrder=5000-Mathf.RoundToInt((y-size/2+.1f)*10);
                visibleTrees[key]=tree;
            }
            else if(currentTiles[idx]==0)
            {
                // Strict check: ONLY land grass (tile 0), NEVER bridges (5, 7, 8) or water (1) or paths (6)
                float ddx=x-size/2f,ddy=y-size/2f;
                float dr=Mathf.Sqrt(ddx*ddx+ddy*ddy);
                if(dr>5.5f)
                {
                    // Coarse 3x3 cell grid: ensures at least 3 tiles separation between decor props
                    int cellX=(x>=0?x:(x-2))/3;
                    int cellY=(y>=0?y:(y-2))/3;
                    int cellHash=((cellX*1274126177)^(cellY*668265263))&0x7FFFFFFF;
                    if((cellHash%100)<35)
                    {
                        int localX=((x%3)+3)%3;
                        int localY=((y%3)+3)%3;
                        if(localX==1&&localY==1)
                        {
                            bool nearObstacle=false;
                            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                            {
                                int nx=x+dx,ny=y+dy;
                                if(nx<0||ny<0||nx>=size||ny>=size){nearObstacle=true;break;}
                                byte t=currentTiles[ny*size+nx];
                                if(t!=0){nearObstacle=true;break;}
                            }
                            if(nearObstacle)continue;
                            V2 decorWorldPos=new V2(x-size/2f+.5f,y-size/2f+.35f);
                            if(State?.Structures!=null&&State.Structures.Any(s=>V2.Distance(s.Pos,decorWorldPos)<2.2f))continue;
                            if(State?.Nodes!=null&&State.Nodes.Any(n=>V2.Distance(n.Pos,decorWorldPos)<2.0f))continue;
                            int key=idx;keepDecor.Add(key);
                            if(!visibleDecor.ContainsKey(key))
                                {
                                    int decorType=(cellHash/1000)%9;
                                    string decorArt=decorType switch
                                    {
                                        0=>"wildflowers",
                                        1=>"stone_pebble",
                                        2=>"mushrooms",
                                        3=>"bush_small",
                                        4=>"bush_wide",
                                        5=>"tall_stump",
                                        6=>"bush_round",
                                        7=>"tree_stump",
                                        _=>"log_fallen"
                                    };
                                    if((decorArt=="bush_wide"||decorArt=="log_fallen")&&x+1<size&&currentTiles[y*size+x+1]!=0)
                                    {
                                        decorArt="bush_small";
                                    }
                                    SpriteRenderer decorSr=null;
                                    while(decorPool.Count>0&&(decorSr==null||decorSr.gameObject==null))decorSr=decorPool.Pop();
                                    if(decorSr!=null&&decorSr.gameObject!=null)decorSr.enabled=true;
                                    else
                                    {
                                        decorSr=new GameObject("GroundDecor").AddComponent<SpriteRenderer>();
                                        if(mapRoot!=null)decorSr.transform.SetParent(mapRoot.transform);
                                        if(spriteMaterial!=null)decorSr.sharedMaterial=spriteMaterial;
                                    }
                                    decorSr.sprite=Primitive(decorArt);
                                    float jx=((cellHash%17)-8)/32f;
                                    float jy=(((cellHash/17)%17)-8)/32f;
                                    decorSr.transform.position=new Vector3(x-size/2f+.5f+jx,y-size/2f+.35f+jy,0);
                                    float decorScale=decorType switch
                                    {
                                        0=>.85f,
                                        1=>.80f,
                                        2=>.90f,
                                        3=>.85f,
                                        4=>.95f,
                                        5=>.90f,
                                        6=>.90f,
                                        7=>.90f,
                                        _=>.95f
                                    };
                                    decorSr.transform.localScale=Vector3.one*decorScale;
                                    decorSr.color=Color.white;
                                    decorSr.sortingOrder=2000-Mathf.RoundToInt((y-size/2f+.35f)*10);
                                    visibleDecor[key]=decorSr;
                                }
                            }
                        }
                    }
            }}
            foreach(var kv in visibleTrees)if(!keepTrees.Contains(kv.Key))dropTrees.Add(kv.Key);
            for(int i=0;i<dropTrees.Count;i++)
            {
                int key=dropTrees[i];
                var sr=visibleTrees[key];
                if(sr!=null&&sr.gameObject!=null){sr.enabled=false;treePool.Push(sr);}
                visibleTrees.Remove(key);
                treeSpawnClock.Remove(key);
                treeBaseScale.Remove(key);
                treeBasePos.Remove(key);
            }
            foreach(var kv in visibleDecor)if(!keepDecor.Contains(kv.Key))dropDecor.Add(kv.Key);
            for(int i=0;i<dropDecor.Count;i++)
            {
                int key=dropDecor[i];
                var sr=visibleDecor[key];
                if(sr!=null&&sr.gameObject!=null){sr.enabled=false;decorPool.Push(sr);}
                visibleDecor.Remove(key);
            }
        }
        void InitStyles()
        {
            if(title!=null)return;
            try
            {
                var osFont=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI","Arial","Tahoma"},14);
                if(osFont!=null)GUI.skin.font=osFont;
            }
            catch{}
            title=new GUIStyle(GUI.skin.label){fontSize=27,fontStyle=FontStyle.Bold,normal={textColor=new Color(1,.92f,.78f)}};
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
        void DrawShadowDualCrossSlash(Effect e)
        {
            if(Event.current.type!=EventType.Repaint)return;
            float prog=Mathf.Clamp01((.3f-e.Life)/.3f);
            float alpha=Mathf.Max(0f,1f-prog*.85f);
            Vector2 from=ScreenPos(e.From);
            Vector2 to=ScreenPos(e.To);
            Vector2 dir=to-from;
            if(dir.sqrMagnitude<.001f)dir=new Vector2(1f,0f);
            Vector2 fwd=dir.normalized;
            Vector2 perp=new Vector2(-fwd.y,fwd.x);

            Vector2 center=Vector2.Lerp(from,to,.65f)+fwd*(prog*18f);

            // Tay chính: chém lướt chéo xuống-phải (góc ~18°)
            float angle1=18f*Mathf.Deg2Rad;
            Vector2 fwd1=fwd*Mathf.Cos(angle1)+perp*Mathf.Sin(angle1);
            Vector2 perp1=-fwd*Mathf.Sin(angle1)+perp*Mathf.Cos(angle1);
            DrawCrescentArc(center,fwd1,perp1,18f+prog*6f,24f+prog*6f,
                new Color(.66f,.33f,.97f,alpha*.40f),
                new Color(.85f,.70f,1f,alpha*.85f),
                new Color(1f,1f,1f,alpha*.95f));

            // Tay phụ: chém lướt chéo lên-phải (góc ~-18°, lùi sau 3px theo nhịp delay)
            float angle2=-18f*Mathf.Deg2Rad;
            Vector2 fwd2=fwd*Mathf.Cos(angle2)+perp*Mathf.Sin(angle2);
            Vector2 perp2=-fwd*Mathf.Sin(angle2)+perp*Mathf.Cos(angle2);
            Vector2 center2=center-fwd*3f;
            DrawCrescentArc(center2,fwd2,perp2,18f+prog*6f,24f+prog*6f,
                new Color(.93f,.28f,.60f,alpha*.35f),
                new Color(.96f,.45f,.73f,alpha*.80f),
                new Color(1f,1f,1f,alpha*.95f));

            // Tâm điểm giao nhau hình chữ X: Vẽ sprite shadow_cross_slash xoay theo hướng chém và phát quang sắc nét
            var slashSprite=Primitive("shadow_cross_slash");
            if(slashSprite!=null&&slashSprite.texture!=null)
            {
                float slashSize=28f+prog*14f;
                var slashRect=new Rect(center.x-slashSize/2,center.y-slashSize/2,slashSize,slashSize);
                var oldMatrix=GUI.matrix;
                float slashAngle=Mathf.Atan2(fwd.y,fwd.x)*Mathf.Rad2Deg;
                GUIUtility.RotateAroundPivot(slashAngle,center);
                var oldCol=GUI.color;
                GUI.color=new Color(1f,1f,1f,alpha*.95f);
                GUI.DrawTexture(slashRect,slashSprite.texture);
                GUI.color=oldCol;
                GUI.matrix=oldMatrix;
            }
        }
        void DrawCrescentArc(Vector2 center,Vector2 forward,Vector2 perp,float rx,float ry,Color outerGlow,Color bodyColor,Color coreColor)
        {
            const int segments=10;
            const float maxAngle=.38f*Mathf.PI;
            Vector2 prevOuter=Vector2.zero,prevBody=Vector2.zero,prevCore=Vector2.zero;
            for(int i=0;i<=segments;i++)
            {
                float t=(float)i/segments;
                float angle=Mathf.Lerp(-maxAngle,maxAngle,t);
                float cosA=Mathf.Cos(angle);
                float sinA=Mathf.Sin(angle);
                Vector2 pt=center+forward*(rx*cosA)+perp*(ry*sinA);
                if(i>0)
                {
                    GUI.color=outerGlow;
                    Line(prevOuter,pt,5.5f);
                    GUI.color=bodyColor;
                    Line(prevBody,pt,2.5f);
                    if(t>=.15f&&t<=.85f)
                    {
                        GUI.color=coreColor;
                        Line(prevCore,pt,1f);
                    }
                }
                prevOuter=pt;prevBody=pt;prevCore=pt;
            }
            GUI.color=Color.white;
        }
        void DrawBossChargeSlash(Effect e)
        {
            if(Event.current.type!=EventType.Repaint)return;
            float prog=Mathf.Clamp01((.3f-e.Life)/.3f);
            float alpha=Mathf.Max(0f,1f-prog*.85f);
            BossType bType=State!=null?State.BossType:BossType.VoidSovereign;
            Color accent=BossAccentColor(bType);
            Color secondary=BossSecondaryColor(bType);
            Color coreWhite=new Color(1f,1f,1f,alpha*.95f);

            float dist=V2.Distance(e.From,e.To);
            if(dist<0.5f)
            {
                Ring(e.To,0.4f+prog*1.6f,new Color(accent.r,accent.g,accent.b,alpha*.85f),3.5f);
                Ring(e.To,0.2f+prog*1.1f,new Color(secondary.r,secondary.g,secondary.b,alpha*.95f),2.2f);
                Ring(e.To,0.05f+prog*0.5f,coreWhite,1.2f);
                return;
            }

            Vector2 from=ScreenPos(e.From);
            Vector2 to=ScreenPos(e.To);
            Vector2 delta=to-from;
            float lenPx=delta.magnitude;
            if(lenPx<1f)return;
            Vector2 fwd=delta/lenPx;
            Vector2 perp=new Vector2(-fwd.y,fwd.x);

            if(bType==BossType.CinderBehemoth)
            {
                // Hỏa Chưởng Tro Tàn: Magma Kamehameha Ray VFX
                float beamW=34f;
                GUI.color=new Color(accent.r,accent.g,accent.b,alpha*.55f);
                Line(from,to,beamW);
                GUI.color=new Color(secondary.r,secondary.g,secondary.b,alpha*.85f);
                Line(from,to,16f);
                GUI.color=coreWhite;
                Line(from,to,5f);
                Ring(e.From,0.6f+prog*1.2f,new Color(secondary.r,secondary.g,secondary.b,alpha*.95f),3.5f);
                Ring(e.To,0.8f+prog*1.8f,new Color(accent.r,accent.g,accent.b,alpha*.90f),4.5f);
                Ring(e.To,0.3f+prog*0.9f,coreWhite,2.0f);
                GUI.color=Color.white;
                return;
            }

            float corridorW=22f;

            // 1. Đa tầng vệt chém hư không rực sáng dọc đường lướt:
            // Outer Glow tím hư không phủ rộng hai bên hành lang
            GUI.color=new Color(accent.r,accent.g,accent.b,alpha*.35f);
            Line(from+perp*(corridorW*.5f),to+perp*(corridorW*.5f),14f);
            Line(from-perp*(corridorW*.5f),to-perp*(corridorW*.5f),14f);

            // Dòng kiếm khí tím đậm ở thân vệt chém
            GUI.color=new Color(accent.r,accent.g,accent.b,alpha*.75f);
            Line(from,to,7.5f);

            // Viền Cyan Starlight sắc nét xé toạc không gian dọc 2 mép
            GUI.color=new Color(secondary.r,secondary.g,secondary.b,alpha*.95f);
            Line(from+perp*corridorW,to+perp*corridorW,2.5f);
            Line(from-perp*corridorW,to-perp*corridorW,2.5f);

            // Lõi trắng Starlight Core xé toạc không gian ở chính giữa
            GUI.color=coreWhite;
            Line(from,to,1.8f);

            // 2. Vệt cào xé bóng đêm: các vết rách không gian đan chéo so le dọc vệt lướt
            for(int k=1;k<=4;k++)
            {
                float t=k*.2f;
                Vector2 mid=Vector2.Lerp(from,to,t);
                float clawAngle=(k%2==0?32f:-32f)*Mathf.Deg2Rad;
                Vector2 clawDir=fwd*Mathf.Cos(clawAngle)+perp*Mathf.Sin(clawAngle);
                Vector2 clawStart=mid-clawDir*(corridorW*1.2f);
                Vector2 clawEnd=mid+clawDir*(corridorW*1.2f);

                GUI.color=new Color(accent.r,accent.g,accent.b,alpha*.40f);
                Line(clawStart,clawEnd,4.5f);
                GUI.color=new Color(secondary.r,secondary.g,secondary.b,alpha*.90f);
                Line(clawStart,clawEnd,2.0f);
                GUI.color=coreWhite;
                Line(Vector2.Lerp(clawStart,clawEnd,.2f),Vector2.Lerp(clawStart,clawEnd,.8f),1.0f);
            }

            // 3. Shockwave hình bán nguyệt dọc đường lướt:
            // Sóng kích động bán nguyệt tiên phong ở đầu mũi lướt
            float headT=Mathf.Clamp01(prog*1.35f);
            Vector2 headPos=Vector2.Lerp(from,to,headT);
            float rxLead=24f+prog*16f;
            float ryLead=38f+prog*20f;
            DrawCrescentArc(headPos,fwd,perp,rxLead,ryLead,
                new Color(accent.r,accent.g,accent.b,alpha*.45f),
                new Color(secondary.r,secondary.g,secondary.b,alpha*.90f),
                coreWhite);

            // Các tầng sóng bán nguyệt gợn sóng lan tỏa phía sau
            if(prog>.08f)
            {
                Vector2 trailPos1=Vector2.Lerp(from,to,.40f);
                DrawCrescentArc(trailPos1,fwd,perp,18f+prog*10f,28f+prog*12f,
                    new Color(accent.r,accent.g,accent.b,alpha*.30f),
                    new Color(secondary.r,secondary.g,secondary.b,alpha*.65f),
                    new Color(1f,1f,1f,alpha*.70f));
            }
            if(prog>.18f)
            {
                Vector2 trailPos2=Vector2.Lerp(from,to,.75f);
                DrawCrescentArc(trailPos2,fwd,perp,20f+prog*12f,32f+prog*14f,
                    new Color(accent.r,accent.g,accent.b,alpha*.25f),
                    new Color(secondary.r,secondary.g,secondary.b,alpha*.55f),
                    new Color(1f,1f,1f,alpha*.60f));
            }

            GUI.color=Color.white;
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
            DrawRelicWorldEffects();
            DrawBossTelegraphs();
            DrawRangeRulers(p);
            foreach(var e in State.Effects){
                if(e.Kind==6){Ring(e.To,.35f+(.3f-e.Life)*2,new Color(.83f,.73f,1,e.Life/.3f));continue;}
                if(e.Kind==7){DrawHarvestChips(e);continue;}
                if(e.Kind==4||e.Kind==5){DrawMeteorEffect(e);continue;}
                if(e.Kind==8){DrawCookingSteamEffect(e);continue;}
                if(e.Kind==9){DrawShadowDualCrossSlash(e);continue;}
                if(e.Kind==3){DrawBossChargeSlash(e);continue;}
                GUI.color=e.Kind==0?Gold:Coral;Line(ScreenPos(e.From),ScreenPos(e.To),3);if(e.Kind>=2)Ring(e.To,1+(.3f-e.Life)*6,GUI.color);GUI.color=Color.white;}
            DrawStructureBars();
            DrawHarvestBars();
            DrawCookingBars();
            DrawBeaconOverhead(p);
            DrawEnemyHealthBars();
            DrawCombatPopups();
            foreach(var q in State.Players)
            {
                if(q.Extracted||q.Ghost||(!p.Ghost&&p.Mad&&q.Id!=p.Id))continue;
                DrawOverheadStatus(q);
            }
            if(!p.Ghost&&!inventoryOpen&&!craftOpen&&!beaconDepositOpen)DrawInteractPrompt(p);
            if(!p.Ghost&&inventoryOpen){DrawInventory(p);return;}
            if(Event.current.type==EventType.Repaint){hudBlockingRects.Clear();hudCapturing=true;}
            DrawFieldHud(p);
            DrawBossHud();
            DrawRelicHud();
            DrawMerchantHud(p);
            float objectiveY=ObjectiveCardY;
            DrawObjectiveCard(p,objectiveY);
            DrawFieldNotices(p,QuestCollapsed?objectiveY+92f:objectiveY+88f);
            if(!p.Ghost&&craftOpen)DrawCraft(p);
            if(!p.Ghost&&beaconDepositOpen)DrawBeaconDepositModal(p);
            hudCapturing=false;
            float fadeAlpha=GetTeleportFadeAlpha();
            if(fadeAlpha>0.001f&&Event.current.type==EventType.Repaint)
                Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.04f,.02f,.08f,fadeAlpha));

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
                int sz=State.Size;
                int tx=(int)Mathf.Floor(world.X+sz/2f);
                int ty=(int)Mathf.Floor(world.Y+sz/2f);
                bool explored=exploredTiles==null||(tx>=0&&tx<sz&&ty>=0&&ty<sz&&exploredTiles[ty*sz+tx]);
                if(!explored)
                {
                    alpha=Mathf.Max(alpha,.96f);
                    Panel(new Rect(x,y,cell,cell),new Color(.015f,.02f,.045f,alpha));
                }
                else
                {
                    Panel(new Rect(x,y,cell,cell),new Color(.01f,.015f,.04f,alpha));
                }
            }
        }
        void UpdateExploration(float dt)
        {
            if(State==null||exploredTiles==null||State.Players==null)return;
            int size=State.Size;
            if(exploredTiles.Length!=size*size)return;
            bool newlyExplored=false;
            var players=State.Players;
            for(int i=0;i<players.Length;i++)
            {
                var p=players[i];
                if(p.Ghost||p.Extracted)continue;
                float r=p.HasHeldTorch?11.5f:p.LampOn?9.5f:6.5f;
                int px=(int)Mathf.Floor(p.Pos.X+size/2f);
                int py=(int)Mathf.Floor(p.Pos.Y+size/2f);
                int ri=Mathf.CeilToInt(r);
                int rSq=ri*ri;
                for(int dy=-ri;dy<=ri;dy++)
                {
                    int ty=py+dy;
                    if(ty<0||ty>=size)continue;
                    int row=ty*size;
                    for(int dx=-ri;dx<=ri;dx++)
                    {
                        if(dx*dx+dy*dy>rSq)continue;
                        int tx=px+dx;
                        if(tx<0||tx>=size)continue;
                        int idx=row+tx;
                        if(!exploredTiles[idx])
                        {
                            exploredTiles[idx]=true;
                            if(fogColors!=null&&idx<fogColors.Length)fogColors[idx]=new Color32(0,0,0,0);
                            newlyExplored=true;
                        }
                    }
                }
            }
            if(State.Structures!=null)
            {
                for(int si=0;si<State.Structures.Length;si++)
                {
                    var s=State.Structures[si];
                    if(s.Kind<=1)
                    {
                        float r=s.Kind==1?9f:5.5f;
                        int px=(int)Mathf.Floor(s.Pos.X+size/2f);
                        int py=(int)Mathf.Floor(s.Pos.Y+size/2f);
                        int ri=Mathf.CeilToInt(r);
                        int rSq=ri*ri;
                        for(int dy=-ri;dy<=ri;dy++)
                        {
                            int ty=py+dy;
                            if(ty<0||ty>=size)continue;
                            int row=ty*size;
                            for(int dx=-ri;dx<=ri;dx++)
                            {
                                if(dx*dx+dy*dy>rSq)continue;
                                int tx=px+dx;
                                if(tx<0||tx>=size)continue;
                                int idx=row+tx;
                                if(!exploredTiles[idx])
                                {
                                    exploredTiles[idx]=true;
                                    if(fogColors!=null&&idx<fogColors.Length)fogColors[idx]=new Color32(0,0,0,0);
                                    newlyExplored=true;
                                }
                            }
                        }
                    }
                }
            }
            if(newlyExplored)
            {
                minimapDirty=true;
                fogDirty=true;
            }
            minimapUpdateTimer+=dt;
            if(minimapDirty&&minimapUpdateTimer>=0.15f)
            {
                minimapUpdateTimer=0f;
                UpdateMinimapTexture();
            }
            fogUpdateTimer+=dt;
            if(fogDirty&&fogUpdateTimer>=0.1f)
            {
                EnsureFogOfWar();
                if(fogTexture!=null&&fogColors!=null)
                {
                    fogUpdateTimer=0f;
                    fogTexture.SetPixels32(fogColors);
                    fogTexture.Apply(false);
                    fogDirty=false;
                }
            }
        }
        public void EnsureFogOfWar()
        {
            if(State==null)return;
            int size=State.Size;
            if(size<=0)return;

            if(emissionMaterial==null||emissionMaterial.shader==null||!emissionMaterial.shader.isSupported)
                EnsureLightingMaterials();

            if(mapRoot==null)
            {
                mapRoot=GameObject.Find("Ember map");
                if(mapRoot==null)mapRoot=new GameObject("Ember map");
            }

            if(exploredTiles==null||exploredTiles.Length!=size*size)
            {
                exploredTiles=new bool[size*size];
                exploredSize=size;
                int centerTile=size/2,sanR=8;
                for(int dy=-sanR;dy<=sanR;dy++)
                    for(int dx=-sanR;dx<=sanR;dx++)
                        if(dx*dx+dy*dy<=sanR*sanR)
                        {
                            int tx=centerTile+dx,ty=centerTile+dy;
                            if(tx>=0&&tx<size&&ty>=0&&ty<size)exploredTiles[ty*size+tx]=true;
                        }
            }

            if(fogRenderer==null)
            {
                if(mapRoot!=null)
                {
                    var existingFog=mapRoot.transform.Find("World Fog Layer");
                    if(existingFog!=null)fogRenderer=existingFog.GetComponent<SpriteRenderer>();
                }
                if(fogRenderer==null)
                {
                    var found=GameObject.Find("World Fog Layer");
                    if(found!=null)fogRenderer=found.GetComponent<SpriteRenderer>();
                }
                if(fogRenderer==null&&mapRoot!=null)
                {
                    var fogGo=new GameObject("World Fog Layer");
                    fogGo.transform.SetParent(mapRoot.transform);
                    fogRenderer=fogGo.AddComponent<SpriteRenderer>();
                }
            }

            bool needRebuildTex=fogTexture==null||fogTexture.width!=size||fogTexture.height!=size||fogColors==null||fogColors.Length!=size*size;
            if(needRebuildTex)
            {
                fogTexture=new Texture2D(size,size,TextureFormat.RGBA32,false);
                fogTexture.filterMode=FilterMode.Bilinear;
                fogTexture.wrapMode=TextureWrapMode.Clamp;
                fogColors=new Color32[size*size];
                for(int i=0;i<fogColors.Length;i++)
                    fogColors[i]=(exploredTiles!=null&&exploredTiles[i])?new Color32(0,0,0,0):new Color32(9,7,18,248);
                fogTexture.SetPixels32(fogColors);
                fogTexture.Apply(false);
                fogDirty=false;
            }

            if(fogRenderer!=null)
            {
                if(fogRenderer.sprite==null||fogRenderer.sprite.texture!=fogTexture)
                    fogRenderer.sprite=Sprite.Create(fogTexture,new Rect(0,0,size,size),new Vector2(.5f,.5f),1);
                fogRenderer.sortingOrder=9000;
                if(emissionMaterial!=null&&fogRenderer.sharedMaterial!=emissionMaterial)
                    fogRenderer.sharedMaterial=emissionMaterial;
            }
        }
        void UpdateMinimapTexture()
        {
            if(minimapTex==null||minimapColors==null||exploredTiles==null||currentTiles==null)return;
            int size=State!=null?State.Size:exploredSize;
            if(minimapColors.Length!=size*size||currentTiles.Length!=size*size)return;
            for(int i=0;i<minimapColors.Length;i++)
            {
                if(!exploredTiles[i])
                {
                    minimapColors[i]=new Color32(11,8,20,255);
                }
                else
                {
                    byte t=currentTiles[i];
                    switch(t)
                    {
                        case 0: minimapColors[i]=new Color32(28,58,36,255); break;
                        case 1: minimapColors[i]=new Color32(15,30,56,255); break;
                        case 2: minimapColors[i]=new Color32(60,56,76,255); break;
                        case 3: minimapColors[i]=new Color32(18,72,36,255); break;
                        case 4: minimapColors[i]=new Color32(6,5,12,255); break;
                        case 5: minimapColors[i]=new Color32(98,70,44,255); break;
                        case 6: minimapColors[i]=new Color32(72,62,52,255); break;
                        case 7: minimapColors[i]=new Color32(130,62,32,255); break;
                        default: minimapColors[i]=new Color32(24,20,32,255); break;
                    }
                }
            }
            minimapTex.SetPixels32(minimapColors);
            minimapTex.Apply(false);
            minimapDirty=false;
        }
        void DrawCraft(Player p)
        {
            DrawCraftingMenu(p);
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
                if(!Profile.Vessels.Contains(selected))UiText(L("Bản Ngã Đặc Biệt · Tỷ lệ triệu hồi: 0,1%","Special Vessel · Summon rate: 0.1%"),small);
                else if(!Profile.LittleMooneAwake)
                {
                    UiText(L("[Ngủ sâu] · Cần Đánh Thức để xuất trận","[Deep Sleep] · Requires Awakening to deploy"),small);
                    UiText(EmberLanguage.Text("Đá Ký Ức Thức Tỉnh")+": "+Profile.AwakeningStones,small);
                    GUI.enabled=Profile.AwakeningStones>0;
                    if(UiButton(L("Đánh thức Little Moone (1 Đá)","Awaken Little Moone (1 Stone)"))&&Profile.AwakenLittleMoone())SaveProfile();
                    GUI.enabled=true;
                }
                else
                {
                    UiText(L("★ Đã Thức Tỉnh: Mưa Sao Băng Trắng [J] (12 thiên thạch tự ngắm, 24s)","★ Awakened: Starlight Meteors [J] (12 auto-aimed meteors, 24s)"),small);
                }
            }
            if(selected==(int)Vessel.Wanderer)
            {
                if(!Profile.WandererAwake)
                {
                    UiText(L("Chưa thức tỉnh · Chưa có kỹ năng chủ động","Unawakened · Active skill locked"),small);
                    UiText(EmberLanguage.Text("Đá Ký Ức Thức Tỉnh")+": "+Profile.AwakeningStones,small);
                    GUI.enabled=Profile.AwakeningStones>0;
                    if(UiButton(L("Thức tỉnh Lữ Hành (1 Đá)","Awaken Wanderer (1 Stone)"))&&Profile.AwakenWanderer())SaveProfile();
                    GUI.enabled=true;
                }
                else
                {
                    UiText(L("★ Đã Thức Tỉnh: Gia Tốc Gió Đêm [J] (+45% Tốc chạy, 10s)","★ Awakened: Swiftwind Aura [J] (+45% Speed, 10s)"),small);
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
        void WarmVesselSprites()
        {
            try
            {
                for(int i=0;i<Catalog.VesselNames.Length;i++)Frame((Vessel)i,"idle",0);
            }
            catch(Exception ex){Trace("vessel.warm_error",ex.Message);}
        }
        readonly object profileSaveLock=new object();
        volatile bool profileSaveInFlight=false;
        string pendingProfileJson=null;

        void SaveProfile(bool asyncWrite=true)
        {
            if(saveBlocked||debugSession)return;
            try
            {
                Profile.SelectedVessel=Profile.CanPlay(selected)?selected:0;
                string json=JsonUtility.ToJson(Profile);
                string pwd=LocalPassword;
                string path=SavePath;

                if(!asyncWrite)
                {
                    lock(profileSaveLock)
                    {
                        byte[] protectedBytes=EmberSave.Protect(json,pwd);
                        EmberSave.WriteAtomic(path,protectedBytes);
                    }
                    return;
                }

                lock(profileSaveLock)
                {
                    pendingProfileJson=json;
                    if(profileSaveInFlight)return;
                    profileSaveInFlight=true;
                }

                RunBackgroundProfileSave(pwd,path);
            }
            catch(Exception ex){notice="Lưu thất bại: "+ex.Message;Trace("save.error",ex.Message);}
        }

        void RunBackgroundProfileSave(string pwd,string path)
        {
            System.Threading.Tasks.Task.Run(()=>
            {
                while(true)
                {
                    string jsonToSave;
                    lock(profileSaveLock)
                    {
                        jsonToSave=pendingProfileJson;
                        pendingProfileJson=null;
                    }

                    if(jsonToSave==null)
                    {
                        lock(profileSaveLock){profileSaveInFlight=false;}
                        break;
                    }

                    try
                    {
                        byte[] protectedBytes=EmberSave.Protect(jsonToSave,pwd);
                        lock(profileSaveLock){EmberSave.WriteAtomic(path,protectedBytes);}
                    }
                    catch(Exception ex)
                    {
                        Debug.LogWarning("Async profile save failed: "+ex.Message);
                        Trace("save.error",ex.Message);
                    }

                    lock(profileSaveLock)
                    {
                        if(pendingProfileJson==null)
                        {
                            profileSaveInFlight=false;
                            break;
                        }
                    }
                }
            });
        }
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
        void OnApplicationPause(bool paused){if(paused&&!netSmoke){SaveActiveRun(false);SaveProfile(false);PlayerPrefs.Save();}}
        void OnApplicationQuit(){if(!netSmoke){SaveActiveRun(false);SaveProfile(false);PlayerPrefs.Save();}try{lan?.Leave();}catch(Exception ex){Debug.LogWarning(ex.Message);}}
        void OnDestroy(){lan?.Dispose();CloseDiagnostics();}
    }

    public sealed class EntityInterpolationBuffer
    {
        public struct EntityState
        {
            public V2 Pos;
            public V2 Vel;
        }

        public sealed class Frame
        {
            public double Timestamp;
            public readonly Dictionary<int, EntityState> Players = new Dictionary<int, EntityState>();
            public readonly Dictionary<int, EntityState> Enemies = new Dictionary<int, EntityState>();
            public readonly Dictionary<int, EntityState> Projectiles = new Dictionary<int, EntityState>();
        }

        readonly List<Frame> frames = new List<Frame>();
        readonly Dictionary<int, V2> playerErrors = new Dictionary<int, V2>();
        readonly Dictionary<int, V2> enemyErrors = new Dictionary<int, V2>();
        readonly Dictionary<int, V2> projErrors = new Dictionary<int, V2>();
        readonly Dictionary<int, V2> lastRenderedPlayer = new Dictionary<int, V2>();
        readonly Dictionary<int, V2> lastRenderedEnemy = new Dictionary<int, V2>();
        readonly Dictionary<int, V2> lastRenderedProj = new Dictionary<int, V2>();
        readonly HashSet<int> extrapolatingPlayers = new HashSet<int>();
        readonly HashSet<int> extrapolatingEnemies = new HashSet<int>();
        readonly HashSet<int> extrapolatingProjs = new HashSet<int>();

        double lastArrival = -1;
        double smoothedJitter = 0.010;
        double targetDelay = 0.066;

        public void Clear()
        {
            frames.Clear();
            playerErrors.Clear();
            enemyErrors.Clear();
            projErrors.Clear();
            lastRenderedPlayer.Clear();
            lastRenderedEnemy.Clear();
            lastRenderedProj.Clear();
            extrapolatingPlayers.Clear();
            extrapolatingEnemies.Clear();
            extrapolatingProjs.Clear();
            lastArrival = -1;
            smoothedJitter = 0.010;
            targetDelay = 0.066;
        }

        public void Push(double arrivalTime, Snapshot s, int localId)
        {
            if(s == null) return;
            if(lastArrival > 0)
            {
                double interval = arrivalTime - lastArrival;
                double jitter = Math.Abs(interval - (1.0 / 30.0));
                smoothedJitter = smoothedJitter * 0.9 + jitter * 0.1;
                targetDelay = Math.Max(0.060, Math.Min(0.080, smoothedJitter * 2.0 + 2.0 * (1.0 / 30.0)));
            }
            lastArrival = arrivalTime;

            var prevFrame = frames.Count > 0 ? frames[frames.Count - 1] : null;
            var frame = new Frame { Timestamp = arrivalTime };

            if(s.Players != null)
            {
                foreach(var p in s.Players)
                {
                    if(p == null || p.Id == localId) continue;
                    V2 vel = p.Move * p.Stats.MoveSpeed;
                    if(prevFrame != null && prevFrame.Players.TryGetValue(p.Id, out var prev))
                    {
                        float dt = (float)(arrivalTime - prevFrame.Timestamp);
                        if(dt > 0.001f && V2.Distance(prev.Pos, p.Pos) < 4.5f)
                        {
                            V2 derivedVel = (p.Pos - prev.Pos) / dt;
                            if(derivedVel.Length > 0.1f) vel = derivedVel;
                        }
                    }
                    frame.Players[p.Id] = new EntityState { Pos = p.Pos, Vel = vel };
                }
            }

            if(s.Enemies != null)
            {
                foreach(var e in s.Enemies)
                {
                    if(e == null) continue;
                    V2 vel = new V2();
                    if(prevFrame != null && prevFrame.Enemies.TryGetValue(e.Id, out var prev))
                    {
                        float dt = (float)(arrivalTime - prevFrame.Timestamp);
                        if(dt > 0.001f && V2.Distance(prev.Pos, e.Pos) < 4.5f)
                        {
                            vel = (e.Pos - prev.Pos) / dt;
                        }
                    }
                    frame.Enemies[e.Id] = new EntityState { Pos = e.Pos, Vel = vel };
                }
            }

            if(s.Projectiles != null)
            {
                foreach(var pr in s.Projectiles)
                {
                    if(pr == null) continue;
                    frame.Projectiles[pr.Id] = new EntityState { Pos = pr.Pos, Vel = pr.Velocity };
                }
            }

            frames.Add(frame);
            while(frames.Count > 15 && arrivalTime - frames[0].Timestamp > 0.5)
                frames.RemoveAt(0);
        }

        public V2 GetPlayerPos(int id, double now, V2 fallback)
        {
            bool wasExtrap = extrapolatingPlayers.Contains(id);
            V2 pos = InterpolateEntity(id, now, f => f.Players, fallback, out bool isExtrap);
            if(isExtrap)
            {
                extrapolatingPlayers.Add(id);
            }
            else
            {
                if(wasExtrap)
                {
                    extrapolatingPlayers.Remove(id);
                    if(lastRenderedPlayer.TryGetValue(id, out var lastVis))
                    {
                        V2 err = lastVis - pos;
                        if(err.Length > 0.03f && err.Length < 4.0f) playerErrors[id] = err;
                    }
                }
                else if(lastRenderedPlayer.TryGetValue(id, out var lastVis))
                {
                    float snapDist = V2.Distance(pos, lastVis);
                    if(snapDist > 0.8f && snapDist < 4.0f)
                    {
                        playerErrors[id] = lastVis - pos;
                    }
                }
            }
            pos = ApplyErrorBlending(id, pos, playerErrors);
            lastRenderedPlayer[id] = pos;
            return pos;
        }

        public V2 GetEnemyPos(int id, double now, V2 fallback)
        {
            bool wasExtrap = extrapolatingEnemies.Contains(id);
            V2 pos = InterpolateEntity(id, now, f => f.Enemies, fallback, out bool isExtrap);
            if(isExtrap)
            {
                extrapolatingEnemies.Add(id);
            }
            else
            {
                if(wasExtrap)
                {
                    extrapolatingEnemies.Remove(id);
                    if(lastRenderedEnemy.TryGetValue(id, out var lastVis))
                    {
                        V2 err = lastVis - pos;
                        if(err.Length > 0.03f && err.Length < 4.0f) enemyErrors[id] = err;
                    }
                }
                else if(lastRenderedEnemy.TryGetValue(id, out var lastVis))
                {
                    float snapDist = V2.Distance(pos, lastVis);
                    if(snapDist > 0.8f && snapDist < 4.0f)
                    {
                        enemyErrors[id] = lastVis - pos;
                    }
                }
            }
            pos = ApplyErrorBlending(id, pos, enemyErrors);
            lastRenderedEnemy[id] = pos;
            return pos;
        }

        public V2 GetProjectilePos(int id, double now, V2 fallback)
        {
            bool wasExtrap = extrapolatingProjs.Contains(id);
            V2 pos = InterpolateEntity(id, now, f => f.Projectiles, fallback, out bool isExtrap);
            if(isExtrap)
            {
                extrapolatingProjs.Add(id);
            }
            else
            {
                if(wasExtrap)
                {
                    extrapolatingProjs.Remove(id);
                    if(lastRenderedProj.TryGetValue(id, out var lastVis))
                    {
                        V2 err = lastVis - pos;
                        if(err.Length > 0.03f && err.Length < 4.0f) projErrors[id] = err;
                    }
                }
                else if(lastRenderedProj.TryGetValue(id, out var lastVis))
                {
                    float snapDist = V2.Distance(pos, lastVis);
                    if(snapDist > 0.8f && snapDist < 4.0f)
                    {
                        projErrors[id] = lastVis - pos;
                    }
                }
            }
            pos = ApplyErrorBlending(id, pos, projErrors);
            lastRenderedProj[id] = pos;
            return pos;
        }

        static float GetDeltaTime()
        {
            try
            {
                var prop = typeof(Time).GetProperty("deltaTime", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if(prop != null)
                {
                    object v = prop.GetValue(null);
                    if(v is float f && f > 0.0001f && f < 0.1f) return f;
                }
            }
            catch(Exception) {}
            return 0.016f;
        }

        V2 ApplyErrorBlending(int id, V2 pos, Dictionary<int, V2> errors)
        {
            if(errors.TryGetValue(id, out var err))
            {
                float dt = GetDeltaTime();
                float decay = (float)Math.Exp(-15.0 * dt);
                err *= decay;
                if(err.Length < 0.005f) errors.Remove(id);
                else errors[id] = err;
                return pos + err;
            }
            return pos;
        }

        V2 InterpolateEntity(int id, double now, Func<Frame, Dictionary<int, EntityState>> getDict, V2 fallback, out bool isExtrapolating)
        {
            isExtrapolating = false;
            if(frames.Count == 0) return fallback;
            double renderTime = now - targetDelay;

            var newest = frames[frames.Count - 1];
            var newestDict = getDict(newest);
            if(!newestDict.TryGetValue(id, out var newestState)) return fallback;

            if(frames.Count == 1 || renderTime >= newest.Timestamp)
            {
                isExtrapolating = true;
                float dtExtrap = Mathf.Clamp((float)(renderTime - newest.Timestamp), 0f, 0.25f);
                return newestState.Pos + newestState.Vel * dtExtrap;
            }

            var oldest = frames[0];
            if(renderTime <= oldest.Timestamp)
            {
                var oldestDict = getDict(oldest);
                return oldestDict.TryGetValue(id, out var oldState) ? oldState.Pos : newestState.Pos;
            }

            for(int i = 0; i < frames.Count - 1; i++)
            {
                var f0 = frames[i];
                var f1 = frames[i + 1];
                if(f0.Timestamp <= renderTime && renderTime <= f1.Timestamp)
                {
                    var d0 = getDict(f0);
                    var d1 = getDict(f1);
                    bool has0 = d0.TryGetValue(id, out var s0);
                    bool has1 = d1.TryGetValue(id, out var s1);
                    if(has0 && has1)
                    {
                        float dt = (float)(f1.Timestamp - f0.Timestamp);
                        if(dt <= 0.0001f || V2.Distance(s0.Pos, s1.Pos) >= 4.5f) return s1.Pos;
                        float t = Mathf.Clamp01((float)((renderTime - f0.Timestamp) / dt));
                        float oneMinusT = 1f - t;
                        float h00 = (1f + 2f * t) * oneMinusT * oneMinusT;
                        float h10 = t * oneMinusT * oneMinusT;
                        float h01 = t * t * (3f - 2f * t);
                        float h11 = t * t * (t - 1f);
                        return s0.Pos * h00 + s0.Vel * (h10 * dt) + s1.Pos * h01 + s1.Vel * (h11 * dt);
                    }
                    if(has1) return s1.Pos;
                    if(has0) return s0.Pos;
                }
            }
            return newestState.Pos;
        }
    }
}
