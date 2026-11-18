using System;
using System.IO;
using UnityEngine;

namespace Ember
{
    public partial class EmberGame
    {
        public bool mapEditorOpen;
        byte editorTile = 0; // 0 Grass, 1 Water, 2 Highland, 3 Tree, 4 Abyss, 5 Bridge, 6 Path
        int editorBrush = 1; // 1: 1x1, 2: 3x3, 3: 5x5
        Vector2Int editorHovered = new Vector2Int(-1, -1);

        static readonly string[] EditorTileNames = {
            "Cỏ (Grass)",
            "Nước (Water - Chặn đi)",
            "Đất Đồi (Highland)",
            "Cây Gỗ (Tree - Chặt được)",
            "Vực Abyss (Border)",
            "Cầu Gỗ (Bridge - Đi trên nước)",
            "Đường Mòn (Dirt Path)"
        };

        static readonly Color[] EditorTileColors = {
            new Color(0.64f, 0.76f, 0.39f),
            new Color(0.25f, 0.65f, 0.85f),
            new Color(0.55f, 0.40f, 0.65f),
            new Color(0.20f, 0.50f, 0.25f),
            new Color(0.12f, 0.08f, 0.16f),
            new Color(0.75f, 0.55f, 0.35f),
            new Color(0.80f, 0.68f, 0.48f)
        };

        void UpdateMapEditor()
        {
            // Disabled in-game to avoid conflict with New Input System
            return;
        }

        void PaintTiles(int cx, int cy, byte tile, int brush)
        {
            if (World == null || currentTiles == null) return;
            int size = State.Size;
            int r = brush == 1 ? 0 : brush == 2 ? 1 : 2;
            bool anyChanged = false;

            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                int tx = cx + dx, ty = cy + dy;
                if (tx < 3 || ty < 3 || tx >= size - 3 || ty >= size - 3) continue;
                int idx = ty * size + tx;
                if (currentTiles[idx] != tile)
                {
                    currentTiles[idx] = tile;
                    World.Tiles[idx] = tile;
                    UpdateMapTileVisual(tx, ty, tile);
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                ApplyMapTexture();
                StreamTrees();
            }
        }

        void ApplyMapTexture()
        {
            if (mapTexture != null && mapColors != null)
            {
                mapTexture.SetPixels32(mapColors);
                mapTexture.Apply();
            }
        }

        void SaveCustomMap()
        {
            try
            {
                if (World == null || World.Tiles == null) return;
                string p = Path.Combine(Application.persistentDataPath, "ember_custom_map.dat");
                File.WriteAllBytes(p, World.Tiles);
                notice = "Đã lưu bản đồ tùy chỉnh vào " + Path.GetFileName(p);
            }
            catch (Exception ex)
            {
                notice = "Lỗi lưu bản đồ: " + ex.Message;
            }
        }

        void LoadCustomMap()
        {
            try
            {
                string p = Path.Combine(Application.persistentDataPath, "ember_custom_map.dat");
                if (!File.Exists(p)) { notice = "Chưa có file bản đồ tùy chỉnh đã lưu!"; return; }
                byte[] data = File.ReadAllBytes(p);
                if (data.Length == World.Tiles.Length)
                {
                    Array.Copy(data, World.Tiles, data.Length);
                    Array.Copy(data, currentTiles, data.Length);
                    BuildMap();
                    notice = "Đã nạp bản đồ tùy chỉnh thành công!";
                }
                else notice = "Kích thước bản đồ đã lưu không khớp (" + data.Length + " vs " + World.Tiles.Length + ")";
            }
            catch (Exception ex)
            {
                notice = "Lỗi nạp bản đồ: " + ex.Message;
            }
        }

        void DrawMapEditorGUI()
        {
            // In-game editor disabled by user request in favor of web-based Tilemap Editor
            return;
        }

        void DrawMapEditorHoverCursor()
        {
            if (cam == null || State == null || editorHovered.x < 0 || editorHovered.y < 0) return;
            int size = State.Size;
            float worldX = editorHovered.x - size / 2f + 0.5f;
            float worldY = editorHovered.y - size / 2f + 0.5f;
            int r = editorBrush == 1 ? 0 : editorBrush == 2 ? 1 : 2;

            Vector3 s0 = cam.WorldToScreenPoint(new Vector3(worldX - 0.5f - r, worldY - 0.5f - r, 0));
            Vector3 s1 = cam.WorldToScreenPoint(new Vector3(worldX + 0.5f + r, worldY + 0.5f + r, 0));
            Rect rScreen = new Rect(s0.x, Screen.height - s1.y, s1.x - s0.x, s1.y - s0.y);

            Color boxCol = EditorTileColors[Mathf.Clamp(editorTile, 0, EditorTileColors.Length - 1)];
            boxCol.a = 0.35f;
            Panel(rScreen, boxCol);
        }
    }
}
