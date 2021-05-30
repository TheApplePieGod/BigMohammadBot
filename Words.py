import wordninja

with open("Data.txt", "r") as f:
    content = f.readlines()

with open("Words.txt", "w") as f:
    for line in content:
        words = wordninja.split(line)
        sentence = ' '.join(words)
        if (sentence != ' ' and sentence != ''):
            f.write(sentence + '\n')
