import os
import shutil

src_dir = "ArtShowcase/raw"
dst_dir = "Builds/UiVerificationProject/Assets/Resources/Ember/Props"
my_props = "My project/Assets/Resources/Ember/Props"

os.makedirs(dst_dir, exist_ok=True)

# Protected assets that must NEVER be overwritten from raw
PROTECTED = {"beacon_crystal_dark.png", "beacon_crystal_lit.png", "beacon.png", "beacon_lit.png", "beacon_dark.png"}

tools_and_props = [
    "axe.png",
    "pickaxe.png",
    "stone_pickaxe.png",
    "crystal_axe.png",
    "energy_drink.png",
    "ore.png",
    "ore_node.png",
    "torch.png",
    "torch_fire.png",
    "torch_ground.png",
    "stick.png",
    "wood.png",
    "stone_material.png",
    "stone_pebble.png",
    "apple.png",
    "chest.png",
    "boss.png",
    "boss_cinder.png",
    "boss_eclipse.png",
    "wall.png",
    "bush_small.png",
    "bush_wide.png",
    "bush_round.png",
    "log_fallen.png",
    "tree_stump.png",
    "tall_stump.png",
    "wildflowers.png",
    "mushrooms.png",
]

for item in tools_and_props:
    src_file = os.path.join(src_dir, item)
    if not os.path.exists(src_file):
        src_file = os.path.join(my_props, item)
    if os.path.exists(src_file):
        dst_file = os.path.join(dst_dir, item)
        shutil.copyfile(src_file, dst_file)
        print(f"Copied {item} -> {dst_dir}")
        
        # Also copy .meta if exists
        meta_src = src_file + ".meta"
        if not os.path.exists(meta_src):
            meta_src = os.path.join(my_props, item + ".meta")
        if os.path.exists(meta_src):
            shutil.copyfile(meta_src, os.path.join(dst_dir, item + ".meta"))

# Add pickaxe_stone.png as alias to stone_pickaxe.png
stone_pick = os.path.join(dst_dir, "stone_pickaxe.png")
if os.path.exists(stone_pick):
    shutil.copyfile(stone_pick, os.path.join(dst_dir, "pickaxe_stone.png"))
    shutil.copyfile(stone_pick, os.path.join(my_props, "pickaxe_stone.png"))
    shutil.copyfile(stone_pick, os.path.join(src_dir, "pickaxe_stone.png"))
    print("Created pickaxe_stone.png alias in Builds, My project, and ArtShowcase/raw")

print("Tool synchronization complete.")
