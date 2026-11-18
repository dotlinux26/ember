using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ember;

namespace Ember.SaveTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=================================================================");
            Console.WriteLine("  EMBER SAVE DATA INSPECTOR & CIPHER TOOL (PBKDF2 + AES-256-CBC)");
            Console.WriteLine("=================================================================\n");

            if (args.Length == 0 || args[0] == "help" || args[0] == "-h" || args[0] == "--help")
            {
                PrintHelp();
                return;
            }

            string cmd = args[0].ToLowerInvariant();
            try
            {
                switch (cmd)
                {
                    case "decrypt":
                    {
                        if (args.Length < 4) { Console.WriteLine("Usage: dotnet run --project tools/SaveTool decrypt <input.ember> <output.json> <password>"); return; }
                        string inFile = args[1];
                        string outFile = args[2];
                        string pwd = args[3];
                        byte[] fileBytes = File.ReadAllBytes(inFile);
                        string plainJson = EmberSave.Unprotect(fileBytes, pwd);
                        // Pretty print
                        using var doc = JsonDocument.Parse(plainJson);
                        string pretty = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(outFile, pretty);
                        Console.WriteLine($"[OK] Đã giải mã thành công file: {inFile}");
                        Console.WriteLine($"[OK] Dữ liệu JSON đọc được lưu tại: {outFile}");
                        break;
                    }
                    case "encrypt":
                    {
                        if (args.Length < 4) { Console.WriteLine("Usage: dotnet run --project tools/SaveTool encrypt <input.json> <output.ember> <password>"); return; }
                        string inFile = args[1];
                        string outFile = args[2];
                        string pwd = args[3];
                        string json = File.ReadAllText(inFile);
                        byte[] cipherBytes = EmberSave.Protect(json, pwd);
                        EmberSave.WriteAtomic(outFile, cipherBytes);
                        Console.WriteLine($"[OK] Đã mã hóa thành công file JSON: {inFile}");
                        Console.WriteLine($"[OK] Xuất file .ember được bảo vệ tại: {outFile}");
                        break;
                    }
                    case "view":
                    {
                        if (args.Length < 3) { Console.WriteLine("Usage: dotnet run --project tools/SaveTool view <file.ember> <password>"); return; }
                        string inFile = args[1];
                        string pwd = args[2];
                        byte[] fileBytes = File.ReadAllBytes(inFile);
                        string plainJson = EmberSave.Unprotect(fileBytes, pwd);
                        using var doc = JsonDocument.Parse(plainJson);
                        string pretty = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
                        Console.WriteLine("--- NỘI DUNG DATA PROFILE HIỆN TẠI (JSON) ---");
                        Console.WriteLine(pretty);
                        Console.WriteLine("---------------------------------------------");
                        break;
                    }
                    case "modify":
                    {
                        if (args.Length < 3) { Console.WriteLine("Usage: dotnet run --project tools/SaveTool modify <file.ember> <password> [--ash N] [--tokens N] [--memory N] [--stones N]"); return; }
                        string file = args[1];
                        string pwd = args[2];
                        byte[] bytes = File.ReadAllBytes(file);
                        string plainJson = EmberSave.Unprotect(bytes, pwd);
                        var node = JsonNode.Parse(plainJson);

                        for (int i = 3; i < args.Length; i++)
                        {
                            if (args[i] == "--ash" && i + 1 < args.Length) node["Ash"] = int.Parse(args[++i]);
                            else if (args[i] == "--tokens" && i + 1 < args.Length) node["Tokens"] = int.Parse(args[++i]);
                            else if (args[i] == "--memory" && i + 1 < args.Length) node["Memory"] = int.Parse(args[++i]);
                            else if (args[i] == "--stones" && i + 1 < args.Length) node["AwakeningStones"] = int.Parse(args[++i]);
                            else if (args[i] == "--unlock-moone") node["LittleMooneAwake"] = true;
                        }

                        string updatedJson = node.ToJsonString();
                        byte[] updatedBytes = EmberSave.Protect(updatedJson, pwd);
                        EmberSave.WriteAtomic(file, updatedBytes);
                        Console.WriteLine($"[OK] Đã cập nhật và mã hóa lại file: {file}");
                        Console.WriteLine($"[OK] Nội dung mới: Ash={node["Ash"]}, Tokens={node["Tokens"]}, Memory={node["Memory"]}, Stones={node["AwakeningStones"]}");
                        break;
                    }
                    default:
                        Console.WriteLine($"Lệnh không hợp lệ: {cmd}");
                        PrintHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LỖI]: {ex.Message}");
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("CÁCH HOẠT ĐỘNG CỦA HỆ THỐNG MÃ HÓA SAVE DATA TRONG EMBER:");
            Console.WriteLine("-----------------------------------------------------------------");
            Console.WriteLine("1. KHI NGƯỜI DÙNG CHƯA TẠO PASS: Dùng khóa định danh phần cứng máy:");
            Console.WriteLine("   LocalPassword = \"Ember-local-v1:\" + SystemInfo.deviceUniqueIdentifier");
            Console.WriteLine("   Mỗi máy tính tự có 1 hardware UUID riêng, game tự load tự lưu mà");
            Console.WriteLine("   người chơi không cần gõ bất kỳ mật khẩu nào.");
            Console.WriteLine("2. KHI XUẤT FILE SAO LƯU (Export): Người chơi tự đặt mật khẩu riêng.");
            Console.WriteLine("   File sao lưu dùng PBKDF2 100.000 vòng SHA-256 + AES-256-CBC + HMAC.");
            Console.WriteLine("3. NẾU QUÊN PASS:");
            Console.WriteLine("   Máy gốc vẫn còn nguyên profile.ember và 5 bản sao lưu trong Backups/.");
            Console.WriteLine("   Người dùng chỉ cần mở game trên máy đó, gõ pass mới rồi xuất lại.");
            Console.WriteLine("\nCÁC LỆNH DÙNG SAVE TOOL:");
            Console.WriteLine("  1. Xem dữ liệu JSON bên trong file save:");
            Console.WriteLine("     dotnet run --project tools/SaveTool view <file.ember> <password>");
            Console.WriteLine("  2. Giải mã file .ember ra file .json để mở bằng Notepad/VS Code:");
            Console.WriteLine("     dotnet run --project tools/SaveTool decrypt <file.ember> <output.json> <password>");
            Console.WriteLine("  3. Mã hóa file .json trở lại thành .ember:");
            Console.WriteLine("     dotnet run --project tools/SaveTool encrypt <input.json> <output.ember> <password>");
            Console.WriteLine("  4. Sửa nhanh các chỉ số (Ash, Tokens, Memory...):");
            Console.WriteLine("     dotnet run --project tools/SaveTool modify <file.ember> <password> --ash 9999 --tokens 99 --unlock-moone");
        }
    }
}
