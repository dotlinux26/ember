with open('d:/Ember/build_all_soulknight.py', 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace(',', '')

with open('d:/Ember/build_all_soulknight.py', 'w', encoding='utf-8') as f:
 f.write(text)

print(build_all_soulknight.py cleaned.)
