using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        const string LastCoopSessionPref="Ember.LastCoopSession";
        [Serializable] sealed class CoopSessionInfo
        {
            public string Address,RoomCode,BrokerAddress,Password,RoomName;
            public bool UsePublicBroker;
            public int RouteMode;
        }
        CoopSessionInfo lastCoopSession;
        int coopReconnectAttempts;
        float coopReconnectTimer;
        bool coopReconnectArmed;
        static readonly float[] CoopReconnectDelays={3f,5f,8f};

        void LoadLastCoopSession()
        {
            lastCoopSession=null;
            if(debugSession)return;
            string raw=PlayerPrefs.GetString(LastCoopSessionPref,"");
            if(string.IsNullOrEmpty(raw))return;
            try
            {
                var parsed=JsonUtility.FromJson<CoopSessionInfo>(raw);
                if(parsed!=null&&(!string.IsNullOrEmpty(parsed.RoomCode)||!string.IsNullOrEmpty(parsed.Address)))
                    lastCoopSession=parsed;
            }
            catch{lastCoopSession=null;}
        }

        void SaveLastCoopSession()
        {
            if(debugSession||host||!network)return;
            lastCoopSession=new CoopSessionInfo
            {
                Address=address,
                RoomCode=joinRoomCode,
                BrokerAddress=brokerAddress,
                Password=password,
                RoomName=State?.RoomName??(usePublicBroker?joinRoomCode:address),
                UsePublicBroker=usePublicBroker,
                RouteMode=(int)routeMode
            };
            PlayerPrefs.SetString(LastCoopSessionPref,JsonUtility.ToJson(lastCoopSession));
            PlayerPrefs.Save();
        }

        void ClearLastCoopSession()
        {
            lastCoopSession=null;
            PlayerPrefs.DeleteKey(LastCoopSessionPref);
            PlayerPrefs.Save();
        }

        bool IsResumableConnectionError(string reason)
        {
            if(string.IsNullOrEmpty(reason))return false;
            if(reason.Contains("Host đã đóng phòng")||reason.Contains("Phòng đã đủ người"))return false;
            return true;
        }

        void ReconnectCoop(bool manual=false)
        {
            if(manual)coopReconnectAttempts=0;
            coopReconnectArmed=false;
            coopReconnectTimer=0;
            if(lastCoopSession!=null)
            {
                usePublicBroker=lastCoopSession.UsePublicBroker;
                address=lastCoopSession.Address??address;
                joinRoomCode=lastCoopSession.RoomCode??joinRoomCode;
                brokerAddress=string.IsNullOrEmpty(lastCoopSession.BrokerAddress)?brokerAddress:lastCoopSession.BrokerAddress;
                password=lastCoopSession.Password??password;
                routeMode=(RouteMode)Mathf.Clamp(lastCoopSession.RouteMode,0,2);
            }
            JoinGame();
            notice=L("Đang kết nối lại phòng cũ (Resume)…","Reconnecting to previous room (Resume)…");
        }

        void TickCoopAutoReconnect(float dt)
        {
            if(!network||host||lan==null)return;
            if(string.IsNullOrEmpty(lan.Error))
            {
                if(lan.LocalId>0&&State!=null)
                {
                    coopReconnectAttempts=0;
                    coopReconnectArmed=false;
                    coopReconnectTimer=0;
                    if(lastCoopSession==null||lastCoopSession.RoomName!=State.RoomName)SaveLastCoopSession();
                }
                return;
            }
            if(lan.Error.Contains("Host đã đóng phòng"))
            {
                ClearLastCoopSession();
                coopReconnectArmed=false;
                return;
            }
            if(!IsResumableConnectionError(lan.Error))return;
            if(!coopReconnectArmed&&coopReconnectAttempts<CoopReconnectDelays.Length)
            {
                coopReconnectArmed=true;
                coopReconnectTimer=CoopReconnectDelays[coopReconnectAttempts];
            }
            if(coopReconnectArmed)
            {
                coopReconnectTimer-=dt;
                if(coopReconnectTimer<=0)
                {
                    coopReconnectAttempts++;
                    coopReconnectArmed=false;
                    ReconnectCoop(false);
                }
            }
        }

        void DrawCoopResumeCard()
        {
            if(debugSession||lastCoopSession==null)return;
            Rect box=GUILayoutUtility.GetRect(180,98,GUILayout.ExpandWidth(true));
            var card=new Rect(box.x+4,box.y,Mathf.Max(140,box.width-8),box.height);
            PixelPanel(card,Color.Lerp(Lilac,Plum,.12f));
            PixelArt(new Rect(card.x+10,card.y+12,38,38),"beacon");
            string endpoint=lastCoopSession.UsePublicBroker
                ?L("Mã phòng ","Code ")+lastCoopSession.RoomCode
                :"IP "+lastCoopSession.Address;
            string roomLabel=string.IsNullOrEmpty(lastCoopSession.RoomName)?endpoint:lastCoopSession.RoomName+" · "+endpoint;
            UiLabel(new Rect(card.x+56,card.y+8,card.width-64,22),L("Trận Co-op gần nhất","Last Co-op match"),label);
            UiLabel(new Rect(card.x+56,card.y+30,card.width-64,20),roomLabel,small);
            float clearW=76;
            float resumeW=Mathf.Max(90,card.width-24-clearW);
            if(UiButton(new Rect(card.x+8,card.y+56,resumeW,34),L("Kết nối lại phòng Co-op (Resume)","Reconnect Co-op (Resume)")))ReconnectCoop(true);
            if(UiButton(new Rect(card.x+16+resumeW,card.y+56,clearW,34),L("Xóa","Clear")))
            {
                ClearLastCoopSession();
                notice=L("Đã xóa phiên Co-op gần nhất.","Cleared last Co-op session.");
            }
            GUILayout.Space(8);
        }

        string localAddresses;
        void Avatar(Rect rect,Vessel vessel)
        {
            DrawSpritePreview(rect,Frame(vessel,"idle",0));
        }
        void DrawConnectionIssue(string reason)
        {
            bool resumable=IsResumableConnectionError(reason);
            var box=new Rect(0,0,Mathf.Min(530,Screen.width-28),resumable?286:228);
            box.center=new Vector2(Screen.width/2f,Screen.height/2f);
            Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.08f,.05f,.13f,.78f));
            PixelPanel(box,FieldPaper);
            PixelArt(new Rect(box.x+15,box.y+16,32,32),"umbra");
            UiLabel(new Rect(box.x+56,box.y+10,box.width-72,42),
                resumable?L("Kết nối bị gián đoạn","Connection interrupted"):L("Không vào được phòng","Could not join room"),label);
            string friendlyReason=reason!=null&&reason.Contains("forcibly closed")
                ?L("Không liên lạc được với Host (Host đã tắt hoặc mất mạng).","Could not reach Host (Host offline or network dropped).")
                :reason;
            UiLabel(new Rect(box.x+16,box.y+58,box.width-32,48),friendlyReason,label);
            if(resumable)
            {
                UiLabel(new Rect(box.x+16,box.y+108,box.width-32,58),
                    L("Hành trang & vũ khí được Host giữ an toàn (rơi 30% tài nguyên tại chỗ cũ). Khi quay lại sẽ hồi sinh tại Hải Đăng (hoặc đấu trường Boss nếu đang giao tranh).",
                      "Your gear & weapons are preserved by the Host (30% raw resources drop where you disconnected). Rejoining respawns you at the Beacon (or Boss Arena if active)."),small);
                string retryStatus=coopReconnectArmed
                    ?L("Tự động kết nối lại (lần ","Auto-reconnecting (attempt ")+(coopReconnectAttempts+1)+"/"+CoopReconnectDelays.Length+L(") sau ",") in ")+Mathf.Max(0,coopReconnectTimer).ToString("0.0")+"s…"
                    :L("Đã hết lượt tự nối lại — bấm nút bên dưới để thử lại.","Auto-reconnect paused — click below to retry.");
                UiLabel(new Rect(box.x+16,box.y+168,box.width-32,24),retryStatus,small);
                float half=(box.width-40)/2f;
                if(UiButton(new Rect(box.x+16,box.yMax-58,half+36,40),L("Kết nối lại ngay (Resume)","Reconnect now (Resume)")))ReconnectCoop(true);
                if(UiButton(new Rect(box.x+24+half+36,box.yMax-58,half-36,40),L("Về sảnh","Sanctuary")))LeaveRoom();
            }
            else
            {
                if(UiButton(new Rect(box.x+16,box.yMax-58,box.width-32,40),L("Về sảnh","Back to Sanctuary")))LeaveRoom();
            }
        }

        void DrawLobby()=>WithSanctuaryTheme(DrawRoomSanctuary);
        void DrawRoomSanctuary()
        {
            DrawSanctuaryBackdrop();
            float width=Mathf.Min(920,Screen.width-32),x=(Screen.width-width)/2f;
            const float top=110,gap=12;
            int capacity=State?.Capacity??roomCapacity;
            int columns=Screen.width>=770?capacity:Mathf.Min(capacity,2);
            columns=Mathf.Max(1,columns);
            int rows=Mathf.CeilToInt(capacity/(float)columns);
            float step=Mathf.Clamp((Screen.height-225f)/rows,112,204);
            float cardWidth=(width-gap*(columns-1))/columns;
            UiLabel(new Rect(x,12,width-105,48),L("Đội giữ lửa","Flamekeepers"),title);
            string room=State==null?L("Đang tìm host…","Connecting to host…"):State.RoomName+"   ·   "+State.Players.Length+" / "+capacity;
            UiLabel(new Rect(x,62,width*.62f,27),room,label);
            if(usePublicBroker)
                UiLabel(new Rect(x+width*.63f,62,width*.37f,28),L("Mã ","Code ")+joinRoomCode+"  ·  "+(lan.RelayActive?"Relay":"P2P"),small);
            if(State?.Sandbox==true)UiLabel(new Rect(x,87,width,20),L("Phòng thử · Tiến trình không được lưu","Test room · Progress is not saved"),small);

            for(int i=0;i<capacity;i++)
            {
                float cardX=x+(i%columns)*(cardWidth+gap);
                float cardY=top+(i/columns)*step;
                var card=new Rect(cardX,cardY,cardWidth,step-gap);
                var player=State?.Players?.FirstOrDefault(member=>member.PartySlot==i+1);
                PixelPanel(card,player?.Id==localId?Lilac:Paper);
                UiLabel(new Rect(card.x+9,card.y+3,card.width-18,22),"P"+(i+1)+(i==0?" · Host":""),small);
                float portrait=Mathf.Min(card.width-16,65,Mathf.Max(30,card.height-88));
                var picture=new Rect(card.center.x-portrait/2,card.y+24,portrait,portrait);
                if(player==null)
                {
                    PixelArt(picture,"core");
                    UiLabel(new Rect(card.x+9,card.yMax-33,card.width-18,27),L("Đang chờ","Waiting"),label);
                    continue;
                }
                Avatar(picture,player.Vessel);
                string name=player.Name+(player.Id==localId?L(" · Bạn"," · You"):"");
                UiLabel(new Rect(card.x+9,card.yMax-62,card.width-18,23),name,label);
                UiLabel(new Rect(card.x+9,card.yMax-40,card.width-18,20),Catalog.VesselNames[(int)player.Vessel],small);
                UiLabel(new Rect(card.x+9,card.yMax-21,card.width-18,20),player.Ready?"Đã sẵn sàng":"Chưa sẵn sàng",small);
            }

            float bottom=top+rows*step;
            float actionWidth=Mathf.Min(340,width*.58f);
            float exitWidth=Mathf.Min(155,width*.38f);
            if(host)
            {
                bool ready=State!=null&&State.Players.Length>0&&State.Players.All(p=>p.Ready)&&lan.BrokerReady;GUI.enabled=ready;
                if(UiButton(new Rect(x,bottom,actionWidth,44),L("Bắt đầu · ","Start · ")+(State?.Players.Length??1)+L(" người"," players")))BeginRoomRun();GUI.enabled=true;
                if(localAddresses==null)try{localAddresses=string.Join(" / ",Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(a=>a.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a)).Select(a=>a.ToString()));}catch{localAddresses="Xem IPv4 của máy trong thiết lập mạng.";}
                UiLabel(new Rect(x,bottom+58,width,55),usePublicBroker?"Chia sẻ mã "+joinRoomCode+" và mật khẩu. Broker: "+brokerAddress+(lan.Error==null?"":"\n"+lan.Error):"IP host: "+localAddresses+"\nBạn bè nhập IP và cùng mật khẩu phòng. Cổng UDP 27841.",small);
            }
            else
            {
                GUI.enabled=Local!=null&&string.IsNullOrEmpty(lan.Error);
                if(UiButton(new Rect(x,bottom,actionWidth,44),readyWanted?"Hủy sẵn sàng":"Sẵn sàng")){readyWanted=!readyWanted;lan.SetReady(readyWanted);}GUI.enabled=true;
                UiLabel(new Rect(x,bottom+58,width,50),string.IsNullOrEmpty(lan.Error)?"Host sẽ bắt đầu khi các thành viên sẵn sàng.":lan.Error,label);
            }
            if(UiButton(new Rect(x+width-exitWidth,bottom,exitWidth,44),host?"Đóng phòng":"Rời phòng"))LeaveRoom();
        }
    }
}
