using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ember
{
    public static class EmberLanguage
    {
        public static bool English;
        static readonly Dictionary<string,string> translations=new Dictionary<string,string>();
        static readonly List<(Regex pattern,string text)> fragments=new List<(Regex,string)>();
        static readonly Dictionary<string,string> cache=new Dictionary<string,string>();
        static EmberLanguage()
        {
            foreach(var row in Data.Split('\n'))
            {
                int tab=row.IndexOf('\t');if(tab<1)continue;string vi=row.Substring(0,tab),en=row.Substring(tab+1).TrimEnd('\r');translations[vi]=en;
            }
            foreach(var pair in translations.OrderByDescending(p=>p.Key.Length))
            {
                string before=char.IsLetterOrDigit(pair.Key[0])?@"(?<![\p{L}\p{N}])":"",after=char.IsLetterOrDigit(pair.Key[pair.Key.Length-1])?@"(?![\p{L}\p{N}])":"";
                fragments.Add((new Regex(before+Regex.Escape(pair.Key)+after),pair.Value));
            }
        }
        public static string Text(string value)
        {
            if(!English||string.IsNullOrEmpty(value))return value;if(translations.TryGetValue(value,out var exact))return exact;
            if(cache.TryGetValue(value,out var found))return found;string result=value;foreach(var entry in fragments)result=entry.pattern.Replace(result,entry.text);
            if(cache.Count>4096)cache.Clear();cache[value]=result;return result;
        }
        const string Data=@"Lữ Hành Vô Danh	Nameless Traveler
Y Sư Ánh Trăng	Moon Mender
Kẻ Đặt Bẫy Hỏa Ngục	Inferno Trapper
Hộ Vệ Cổ Ngữ	Rune Warden
Bóng Ma Không Gian	Space Wraith
Bụi quang	Lumen dust
Lõi	Core
Mảnh tối	Umbra shard
Gỗ	Wood
Quặng	Ore
Thần Kinh Thép	Steel Nerves
Lưng Trâu	Strong Back
Bước Chân Bóng Ma	Ghost Step
Kẻ Phòng Xa	Prepared
Thợ Mỏ Lão Luyện	Veteran Miner
Tia Sáng Cuối	Last Light
Lương Y Bất Đắc Dĩ	Reluctant Healer
Đuốc	Torch
Pháo sáng	Flare
Thuốc an thần	Sedative
Đá hồi sinh	Revival stone
Tường	Wall
Bẫy điện	Shock trap
Rương chung	Shared chest
Đèn Hỏa Ngục	Inferno lantern
Đèn Thánh Tích	Relic lantern
Rìu Quang Tinh	Crystal axe
Song Đao Bóng Đêm	Shadow blades
Nỏ Tinh Thể	Crystal crossbow
Súng Quang Năng	Lumen gun
Khiên Gương	Mirror shield
Thu thập lõi, nạp hải đăng, giữ lửa.	Gather cores, charge the beacon, keep the flame alive.
Tia sáng cuối — 5 giây!	Last Light — 5 seconds!
Một thực thể lớn đang tiến về hải đăng.	A massive entity is approaching the beacon.
Cổng đã mở! Về hải đăng và nhấn E.	The portal is open! Return to the beacon and press E.
Bóng tối nuốt chửng cả nhóm.	Darkness has consumed the team.
 trao đèn cho 	 passed the lantern to 
NHẬT THỰC — bảo vệ hải đăng 90 giây!	ECLIPSE — defend the beacon for 90 seconds!
Ảo ảnh vỡ tung.	The mirage explodes.
 đã rút lui.	 extracted safely.
Đã chế tạo: 	Crafted: 
Thực thể đã gục — nhận mảnh ký ức khi kết thúc trận.	The entity has fallen — memory shards awarded after the run.
Thu được mảnh bóng tối.	Umbra shards dropped.
 trở thành hồn ma. Hồi sinh tại hải đăng.	 became a wisp. Revive them at the beacon.
Thể lực	Stamina
Hoảng loạn	Dread
Nhiên liệu	Fuel
Hải đăng	Beacon
Chế tạo [Tab]	Craft [Tab]
WASD đi • K đánh • J kỹ năng • Space lướt • E thu thập/rút lui • B nạp lõi • Q nạp đèn • M đổi chuột • R vòng ngắm	WASD move • K attack • J skill • Space dash • E gather/extract • B deposit • Q refuel • M mouse mode • R ranges
    Tải: 	    Load: 
  [2] Pháo 	  [2] Flare 
  [3] An thần 	  [3] Sedative 
  [4] Hồi sinh 	  [4] Revive 
 • T bật/tắt đèn • L trao đèn • 	 • T toggle lamp • L pass lamp • 
Lõi hải đăng: 	Beacon charge: 
% — mang lõi về trung tâm, nhấn B.	% — bring cores to the beacon and press B.
NHẬT THỰC	ECLIPSE
HỒN MA — đồng đội cần đá hồi sinh tại hải đăng.	WISP — an ally needs a revival stone at the beacon.
Dơi cảnh báo: có quái vật ở gần!	Bat warning: enemies nearby!
Mèo tinh thể: lõi cách 	Crystal cat: nearest core is 
 ô	 tiles away
ĐÃ RÚT LUI	EXTRACTED
NGỌN LỬA ĐÃ TẮT	THE FLAME HAS DIED
Tiến trình đã lưu. Tro: 	Progress saved. Ash: 
 • Ký ức: 	 • Memory: 
 • Xu: 	 • Tokens: 
Trở về Sanctuary	Return to Sanctuary
Thắp sáng hải đăng	Awaken the beacon
Tử thủ	Defend
Cổng đã mở	Portal open
CHẾ TẠO	CRAFTING
Sanctuary · thắp sáng, tử thủ, trở về	Sanctuary · awaken, defend, return
Tắt bộ gõ tiếng Việt khi điều khiển bằng WASD.	Disable text composition / IME when using WASD.
Lữ hành	Traveler
Y sư	Mender
Bẫy bom	Trapper
Hộ vệ	Warden
Bóng ma	Wraith
 • Chưa mở khóa	 • Locked
Tên phòng	Room name
Số người tối đa (vẫn bắt đầu được với 1 người)	Room capacity (you can start with just one player)
Internet · mã phòng	Internet · room code
Chơi một mình	Play solo
TẠO PHÒNG LAN / VPN	CREATE ROOM
IP broker (dịch vụ phải được triển khai trước)	Broker IP (service must be deployed first)
Mã phòng để tham gia	Room code to join
IP máy host	Host IP
Mật khẩu phòng	Room password
Vào phòng	Join room
Tiếp tục	Resume
KÝ ỨC CÒN LẠI	MEMORIES REMAIN
Tro 	Ash 
  •  Ký ức 	  •  Memory 
  •  Xu 	  •  Tokens 
Bản ngã · 3 xu	Vessel · 3 tokens
Mũ · 3 xu	Hat · 3 tokens
Pet · 3 xu	Pet · 3 tokens
Chưa đủ xu.	Not enough tokens.
Triệu hồi: 	Summoned: 
Mẫu #	Variant #
Đặc quyền: 	Perks: 
Mở ô perk · 5 ký ức	Unlock perk slot · 5 memory
 · 30 tro	 · 30 ash
Mũ: 	Hat: 
Sao lưu .ember — mật khẩu cá nhân	.ember backup — personal password
Xuất .ember	Export .ember
Nhập .ember	Import .ember
Thư mục	Folder
Không đọc được save; giữ nguyên file gốc và khóa ghi tự động. 	Could not read save; original retained and autosave disabled. 
Profile không hợp lệ.	Invalid profile.
Profile chứa lựa chọn không hợp lệ.	Profile contains an invalid selection.
Lưu thất bại: 	Save failed: 
Dùng mật khẩu sao lưu ít nhất 8 ký tự.	Use at least 8 characters for the backup password.
Đã giữ bản sao trong Backups.	Backup saved in the internal vault.
Đã xuất và giữ bản sao trong Backups.	Exported and copied to the internal backup vault.
Phòng đã mở. Có thể bắt đầu với 1 người hoặc chờ bạn bè.	Room created. Start alone or wait for friends.
Mã phòng gồm 6 ký tự.	A room code has 6 characters.
Đang kết nối LAN/VPN...	Connecting to LAN/VPN...
Chuột phải: đi đến điểm. WASD hủy đường đi.	Right-click to move. WASD cancels the route.
Điều khiển bàn phím.	Keyboard controls enabled.
Rời phòng	Leave room
Đang chờ host... ESC để quay lại.	Waiting for host... ESC to return.
Không nhận được xác nhận từ host.	No acknowledgement from host.
Phòng đã đủ người.	Room is full.
Trận đã bắt đầu; hãy chờ phòng mới.	The run has started; wait for a new room.
Yêu cầu vào phòng không hợp lệ.	Invalid join request.
Host đã đóng phòng.	The host closed the room.
EMBER  /  ĐỘI GIỮ LỬA	EMBER  /  KEEPERS OF THE FLAME
Đang tìm host...	Looking for the host...
 người	 players
Mã phòng: 	Room code: 
  •  BẠN	  •  YOU
Đã sẵn sàng	Ready
Chưa sẵn sàng	Not ready
Ô đã đóng	Slot closed
Đang chờ bạn đồng hành...	Waiting for a companion...
Bắt đầu với 	Start with 
 người	 players
Xem IPv4 của máy trong thiết lập mạng.	Find the host IPv4 in network settings.
Chia sẻ mã 	Share code 
 và mật khẩu. Broker: 	 and the room password. Broker: 
Bạn bè nhập IP và cùng mật khẩu phòng. Cổng UDP 27841.	Friends enter this IP and the same room password. UDP port 27841.
Hủy sẵn sàng	Not ready
Sẵn sàng	Ready
Host sẽ bắt đầu khi các thành viên sẵn sàng.	The host starts when everyone is ready.
Đóng phòng	Close room
Rời phòng	Leave room
Bản sao lưu nội bộ	INTERNAL BACKUPS
  •  Xem trước phục hồi	  •  Preview restore
Mở thư mục chứa bản xuất gần nhất	Open the latest export in its folder
Sao chép mã khôi phục nhanh	Copy quick recovery code
Đã sao chép bản sao mã hóa. Vẫn cần mật khẩu để phục hồi.	Encrypted backup copied. A password is still required to restore it.
Dán mã khôi phục (bản sao vẫn được mã hóa)	Paste recovery code (backup remains encrypted)
Xem trước từ mã khôi phục	Preview recovery code
Mã khôi phục	Recovery code
PHỤC HỒI KÝ ỨC	RESTORE MEMORIES
  •  Ký ức: 	  •  Memory: 
  •  Xu: 	  •  Tokens: 
Số trận: 	Runs: 
  •  Rút lui: 	  •  Extractions: 
Bản ngã đã mở: 	Unlocked vessels: 
Thay tiến trình hiện tại bằng bản sao này?	Replace current progress with this backup?
PHỤC HỒI	RESTORE
Đã phục hồi. Tiến trình cũ được giữ trong Backups.	Restored. Previous progress was retained in Backups.
HỦY	CANCEL
Sai mật khẩu hoặc file đã bị thay đổi.	Wrong password or modified backup file.
Không tìm thấy phòng.	Room not found.
Sai mật khẩu phòng.	Wrong room password.
Mã phòng đã tồn tại.	Room code already exists.
Máy ghép phòng đang đầy.	The room broker is full.
100 HP • 100 thể lực • Không có kỹ năng chủ động.	100 HP • 100 stamina • No active skill.
90 HP • Hồi 35 HP, xóa 40% hoảng loạn • Hồi chiêu 25 giây.	90 HP • Heal 35 HP, remove 40 dread • 25-second cooldown.
100 HP • +10% tải trọng • Bom diện rộng, làm mù 3 giây • Hồi chiêu 20 giây.	100 HP • +10% capacity • Area bomb, 3-second blind • 20-second cooldown.
140 HP • Chậm hơn 5% • Đẩy lùi và tạo giáp ảo 4 giây • Hồi chiêu 18 giây.	140 HP • 5% slower • Knockback and 4-second armor • 18-second cooldown.
85 HP • Nhanh hơn 15% • Đưa bản thân và một đồng đội sát cạnh về hải đăng • Hồi chiêu 60 giây.	85 HP • 15% faster • Return to the beacon with one adjacent ally • 60-second cooldown.
Giảm 25% tốc độ tăng hoảng loạn.	Dread rises 25% more slowly.
Tăng 30% tải trọng.	Increase carrying capacity by 30%.
Giảm 20% thể lực tiêu hao khi lướt.	Dashes consume 20% less stamina.
Khởi đầu với 2 pháo sáng và 1 thuốc an thần.	Start with 2 flares and 1 sedative.
Khởi đầu với cuốc sắt.	Start with an iron pickaxe.
Đèn cạn nhiên liệu vẫn bùng sáng 5 giây cuối.	The lantern shines for 5 final seconds after fuel runs out.
Giảm 50% sát thương friendly fire gây ra khi bị ảo giác.	Deal 50% less friendly-fire damage while hallucinating.
Mũ Rơm	Straw Hat
Mũ Phù Thủy	Witch Hat
Vương Miện Nến Chảy	Melting Candle Crown
Mặt Nạ Hề Hư Không	Void Jester Mask
Đom Đóm Béo	Plump Firefly
Dơi Bóng Đêm	Night Bat
Mèo Tinh Thể	Crystal Cat
Hồn Ma Nhỏ	Little Ghost
+5% tốc độ khai thác.	+5% gathering speed.
+8% hồi thể lực.	+8% stamina regeneration.
Sáng bán kính 1 ô, giảm 10% tốc độ tăng hoảng loạn.	Light a 1-tile radius; dread rises 10% more slowly.
Hoảng loạn từ 90%: phát tiếng cười và tăng 15% sát thương.	At 90% dread: laugh and deal 15% more damage.
Tự nhặt bụi quang ở gần.	Automatically collect nearby lumen dust.
Cảnh báo quái vật ở gần.	Warn of nearby enemies.
Mỗi 30 giây chỉ hướng tới mỏ lõi gần nhất.	Point to the nearest core deposit every 30 seconds.
Giữ đốm sáng tại vị trí chủ nhân chết.	Leave a light at the owner's death location.
Phổ thông	Common
Đặc biệt	Special
Ngủ đông	Dormant
Tỷ lệ triệu hồi: 1%	Summon chance: 1%
Đá Ký Ức Thức Tỉnh	Awakening Memory Stone
Thức tỉnh Little Moone	Awaken Little Moone
Nhận 1 Đá Ký Ức Thức Tỉnh	Received 1 Awakening Memory Stone
100 HP • Mưa sao băng tự ngắm • Hồi chiêu 24 giây.	100 HP • Auto-targeted meteor shower • 24-second cooldown.
Hiếm	Rare
Sử thi	Epic
Tiến trình đã lưu. Đang chờ đồng đội rút lui.	Progress saved. Waiting for teammates to extract.
Không thể kết nối broker. Kiểm tra IP và cổng UDP 27842.	Cannot reach broker. Check IP and UDP port 27842.
Không nhận được xác nhận từ host.	No acknowledgement from the host.
Mất kết nối host. Hãy kiểm tra địa chỉ và mật khẩu phòng.	Host connection lost. Check the address and room password.
Pet	Pet";
    }
}
