using UnityEngine;

namespace Ember
{
    public sealed partial class EmberGame
    {
        int runAsh,runMemory,runTokens,runStones;
        static readonly string[] RewardIcons={"ash","memory","token","awakening"};

        void RecordRunReward(Player player)
        {
            int ash=Profile.Ash,memory=Profile.Memory,tokens=Profile.Tokens,stones=Profile.AwakeningStones;
            Profile.Reward(State,player);
            runAsh=Profile.Ash-ash;
            runMemory=Profile.Memory-memory;
            runTokens=Profile.Tokens-tokens;
            runStones=Profile.AwakeningStones-stones;
            if(runStones>0)Trace("reward.awakening_stone","count="+runStones);
            SaveProfile();
            if(!network)ClearSavedRun();
            else ClearLastCoopSession();
            rewarded=true;
        }

        void DrawRunResult(Player player,bool awaitingTeam)
        {
            bool success=awaitingTeam||State.Phase==Phase.Won;
            float width=Mathf.Min(Screen.width-30,520);
            var box=new Rect((Screen.width-width)/2,(Screen.height-340)/2,width,340);
            Panel(new Rect(0,0,Screen.width,Screen.height),new Color(.05f,.035f,.08f,.68f));
            PixelPanel(box,FieldPaper);
            var banner=new Rect(box.x+10,box.y+10,box.width-20,120);
            PixelArt(banner,"sanctuary",ScaleMode.ScaleAndCrop);
            if(!success)Panel(banner,new Color(.12f,.06f,.18f,.57f));
            DrawSpritePreview(new Rect(banner.center.x-50,banner.y+6,100,100),
                player.Ghost?Primitive("pet_ghost"):Frame(player.Vessel,"idle",(int)(Time.unscaledTime*6)));
            UiLabel(new Rect(box.x+16,box.y+132,width-32,44),success
                ?player.Extracted?L("Đã rút lui","Extracted"):L("Đồng đội đã rút lui","Team extracted")
                :L("Ngọn lửa đã tắt","The flame went out"),title);
            string detail=awaitingTeam?L("Đang chờ đồng đội rút lui","Waiting for teammates to extract"):
                Mathf.FloorToInt(State.Time/60).ToString("00")+":"+Mathf.FloorToInt(State.Time%60).ToString("00");
            UiLabel(new Rect(box.x+16,box.y+175,width-32,23),detail,small);

            if(rewarded)
            {
                int[] amounts={runAsh,runMemory,runTokens,runStones};
                string[] captions={L("Tro","Ash"),L("Ký ức","Memory"),L("Định Mệnh","Tokens"),L("Đá thức tỉnh","Awakening")};
                float step=(width-28)/4;
                for(int i=0;i<amounts.Length;i++)
                {
                    var card=new Rect(box.x+14+i*step,box.y+200,step-4,72);
                    PixelPanel(card,new Color(.21f,.15f,.32f));
                    PixelArt(new Rect(card.x+6,card.y+6,24,24),RewardIcons[i]);
                    UiLabel(new Rect(card.x+32,card.y+6,card.width-34,25),"+"+amounts[i],small);
                    UiLabel(new Rect(card.x+5,card.y+37,card.width-10,30),captions[i],small);
                }
            }

            if(!awaitingTeam||!host)
                if(UiButton(new Rect(box.x+16,box.y+288,width-32,36),L("Về sảnh","Back to Sanctuary")))LeaveRoom();
        }
    }
}
