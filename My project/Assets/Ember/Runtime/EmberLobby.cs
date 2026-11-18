using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        string localAddresses;
        void Avatar(Rect rect,Vessel vessel)
        {
            DrawSpritePreview(rect,Frame(vessel,"idle",0));
        }
        void DrawConnectionIssue(string reason)
        {
            var box=new Rect(0,0,Mathf.Min(490,Screen.width-28),224);
            box.center=new Vector2(Screen.width/2f,Screen.height/2f);
            Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.08f,.05f,.13f,.75f));
            PixelPanel(box,FieldPaper);
            PixelArt(new Rect(box.x+15,box.y+16,32,32),"umbra");
            UiLabel(new Rect(box.x+56,box.y+10,box.width-72,45),
                Local==null?L("Không vào được phòng","Could not join room"):L("Kết nối bị gián đoạn","Connection interrupted"),label);
            UiLabel(new Rect(box.x+16,box.y+63,box.width-32,89),reason,label);
            if(UiButton(new Rect(box.x+16,box.yMax-58,box.width-32,40),L("Về sảnh","Back to Sanctuary")))LeaveRoom();
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
