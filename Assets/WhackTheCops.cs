using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using rnd = UnityEngine.Random;
using KModkit;
public class WhackTheCops : MonoBehaviour {
    public Sprite[] textures;
    public SpriteRenderer[] cops;
    enum WhackaCop
    {
        Red,
        Cyan,
        Magenta,
        Normal,
        Grey,
        Lime,
        Blue,
        Yellow,
        Black,
    }
    int[][] table = new int[][]{
        new int[] {0,1,2,3,4,5},
        new int[] {6,5,4,0,7,8},
        new int[] {1,0,6,8,5,2},
        new int[] {7,3,5,1,6,4},
        new int[] {4,2,0,7,8,3},
        new int[] {3,6,8,2,1,7}, };
    struct Rule
    {
        public Rule(int a, WhackaCop[] b, int c, bool d)
        {
            moduleIndex = a;
            colors = b;
            solutionIndex = c;
            reversed = d;
        }
        public int moduleIndex;
        public WhackaCop[] colors;
        public int solutionIndex;
        public bool reversed;
    }
    Rule[] rules = new Rule[] {new Rule(0, new WhackaCop[] {WhackaCop.Red, WhackaCop.Normal}, 3, false),
        new Rule(1, new WhackaCop[] {WhackaCop.Normal, WhackaCop.Grey}, 1, true),
        new Rule(3, new WhackaCop[] {WhackaCop.Lime, WhackaCop.Cyan}, 2, true),
        new Rule(2, new WhackaCop[] {WhackaCop.Blue, WhackaCop.Yellow}, 3, true),

    };
    List<int> tableIndices = new List<int>();
    List<int> puzzleIndices = new List<int>();
    List<int> solutionIndices= new List<int>();
    bool otherwise;
    int stage;
    public KMSelectable[] copSelectables;
    public KMBombModule module;
    public KMAudio sound;
    int moduleID;
    int moduleIDCounter = 1;
    bool solved;

	void Awake () {
        moduleID = moduleIDCounter++;
		for (int i = 0; i < 4; i++)
        {
            int j = i;
            copSelectables[j].OnInteract += delegate { PressCop(j); return false; };
        }
        PickCops();
        EvaluateRules();
	}
	
	void PickCops()
    {
        int rowIndex = rnd.Range(0, 5);
        int colIndex = rnd.Range(0, 5);
        tableIndices.Add(table[rowIndex][colIndex]);
        tableIndices.Add(table[rowIndex][colIndex + 1]);
        tableIndices.Add(table[rowIndex + 1][colIndex + 1]);
        tableIndices.Add(table[rowIndex + 1][colIndex]);
        puzzleIndices = tableIndices.ToList();
        puzzleIndices.Shuffle();
        for (int i = 0; i < 4; i++)  cops[i].sprite = textures[puzzleIndices[i]]; 
        Debug.LogFormat("[Whack The Cops #{0}] The chosen Whacka Cops are {1}, {2}, {3}, and {4}.", moduleID, ((WhackaCop)puzzleIndices[0]).ToString("g"), ((WhackaCop)puzzleIndices[1]).ToString("g"), ((WhackaCop)puzzleIndices[2]).ToString("g"), ((WhackaCop)puzzleIndices[3]).ToString("g"));
    }
    void EvaluateRules()
    {
        solutionIndices = tableIndices.ToList();
        foreach (Rule i in rules)
        {
            if (i.colors.Contains( (WhackaCop) puzzleIndices[i.moduleIndex]))
            {
                if (i.reversed) solutionIndices.Reverse();
                solutionIndices = Rotate(solutionIndices, i.solutionIndex);
                break;
            }
        }
        Debug.LogFormat("[Whack The Cops #{0}] The Whacka Cops in the order they should be pressed are {1}, {2}, {3}, and {4}.", moduleID, ((WhackaCop)solutionIndices[0]).ToString("g"), ((WhackaCop)solutionIndices[1]).ToString("g"), ((WhackaCop)solutionIndices[2]).ToString("g"), ((WhackaCop)solutionIndices[3]).ToString("g"));
    }

    List<T> Rotate<T>(List<T> list, int offset)
    {
        return list.Skip(offset).Concat(list.Take(offset)).ToList();
    }

    void PressCop(int index)
    {
        if (!solved)
        {
            copSelectables[index].AddInteractionPunch();
            Debug.LogFormat("[Whack The Cops #{0}] You pressed {1}.", moduleID, ((WhackaCop)puzzleIndices[index]).ToString("g"));
            if (puzzleIndices[index] == solutionIndices[stage])
            {
                stage++;
                Debug.LogFormat("[Whack The Cops #{0}] That was correct.", moduleID);
                sound.PlaySoundAtTransform("Correct", transform);
                if (stage == 4)
                {
                    module.HandlePass();
                    solved = true;
                    Debug.LogFormat("[Whack The Cops #{0}] That was correct. Module solved.", moduleID);
                    sound.PlaySoundAtTransform("Solve", transform);
                }
            }
            else
            {
                module.HandleStrike();
                StopAllCoroutines();
                Debug.LogFormat("[Whack The Cops #{0}] That was incorrect. Strike!", moduleID);
                PickCops();
                EvaluateRules();
            }
        }
    }
#pragma warning disable 414
    private string TwitchHelpMessage = "use '!{0} 1234' to press the Cops in reading order.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        string validcmds = "1234";
        if (command.Contains(' '))
        {
            yield return "sendtochaterror @{0}, invalid command.";
            yield break;
        }
        else
        {
            for (int i = 0; i < command.Length; i++)
            {
                if (!validcmds.Contains(command[i]))
                {
                    yield return "sendtochaterror Invalid command.";
                    yield break;
                }
            }
            for (int i = 0; i < command.Length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    if (command[i] == validcmds[j])
                    {
                        yield return null;
                        yield return new WaitForSeconds(1f);
                        copSelectables[j].OnInteract();
                    }
                }
            }
        }
    }
}

