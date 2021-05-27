using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NonbinaryPuzzle;
using UnityEngine;

enum Colors
{
    Gray,
    Yellow,
    White,
    Purple,
    Black
}

public class NonbinaryPuzzleScript: MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;

    public KMSelectable resetButton;
    public KMSelectable[] buttons;
    public Material[] materials;

    private bool moduleSolved;
    bool whiteText; //Controls reset button flipping
    float flipProgress;
    Coroutine isFlipping;
    private Color black = new Color(0.211f, 0.211f, 0.211f);
    private Color white = new Color(1, 1, 1);

    int[] grid = new int[36].Select(x => x = 0).ToArray();
    int[] givens = new int[36].Select(x => x = 0).ToArray();
    int[] solution = new int[36];
    const int size = 36;

    private void Awake()
    {
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { ButtonToggle(Array.IndexOf(buttons, button)); return false; };
        resetButton.OnInteract += delegate ()
        {
            if (isFlipping != null) StopCoroutine(isFlipping);
            isFlipping = StartCoroutine(ResetFlip());
            return false;
        };
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            whiteText = true;
            flipProgress = 1;
            resetButton.GetComponent<MeshRenderer>().material.color = black;
            resetButton.GetComponentInChildren<TextMesh>().color = white;
        }
    }
    private void Start()
    {

        //solution = GeneratePuzzle().Select(x => x + 1).ToArray();
        Debug.Log(ValidGrid(new int[] {     3,2,1,0,1,0,
    2,1,3,2,0,3,
    1,0,1,0,3,2,
    3,2,0,3,0,1,
    1,0,3,2,1,0,
    0,3,2,1,3,2
        }, true));
        Debug.Log(ValidGrid(solution, true));
        for (int i = 0; i < 36; i++)
        {
            buttons[i].GetComponent<MeshRenderer>().material = materials[solution[i]];
        }
    }

    int[] GeneratePuzzle()
    {
        
        int[] start;
        do
        {
            start = "000000000000000000111111111111111111".Select(x => x - '0').ToArray().Shuffle();
        } while (!ValidGrid(start, false));
        int[] second = new int[36];
        do
        {
            for (int i = 0; i < 36; i++)
            {
                second[i] = start[i];
                if (UnityEngine.Random.Range(0, 2) == 0)
                    second[i] += 2;
            }
        } while (!ValidGrid(second, true));

        return second;
        
    }

    bool ValidGrid(int[] board, bool colors)
    {
        /*    if (board.Count(x => x % 2 == 0) != board.Count(x => x % 2 == 1)) //Checks if wk = yp
                return false;
            IEnumerable<int[]> allRows = Enumerable.Range(0, 6).Select(x => Row(board, x)); 
            IEnumerable<int[]> allCols = Enumerable.Range(0, 6).Select(x => Col(board, x));
            if (allRows.Any(x => Enumerable.Range(0,2).Any(y => x.Select(z => z = z % 2).Count(z => z == y) < 2))) //Checks if there are less than 2 of black/white or less than 2 of yellow/purple
                return false;
            if (allCols.Any(x => Enumerable.Range(0, 2).Any(y => x.Select(z => z = z % 2).Count(z => z == y) < 2)))
                return false;
            if (!colors) //If we aren't using 4 colors, we don't care about adjacency yet.
                return true;

            if (allRows.Any(x => Enumerable.Range(0, 4).Any(y => !x.Contains(y)))) //Checks if any row has a missing color;
                return false;
            if (allCols.Any(x => Enumerable.Range(0, 4).Any(y => !x.Contains(y)))) 
                return false;
            if (Enumerable.Range(0, 36).Any(x => GetAdjacents(x).Select(y => board[y]).Any(y => y == board[x])))
                return false;
            return true;*/
            if (board.Count(x => x % 2 == 0) != board.Count(x => x % 2 == 1)) //Checks if wk = yp
        {
            Debug.LogFormat("Unequal count of {0} colors and {1} monocolors", board.Count(x => x % 2 == 0), board.Count(x => x % 2 == 1));
                return false;
        }
            IEnumerable<int[]> allRows = Enumerable.Range(0, 6).Select(x => Row(board, x)); 
            IEnumerable<int[]> allCols = Enumerable.Range(0, 6).Select(x => Col(board, x));
            if (allRows.Any(x => Enumerable.Range(0,2).Any(y => x.Select(z => z = z % 2).Count(z => z == y) < 2))) //Checks if there are less than 2 of black/white or less than 2 of yellow/purple
        {
            Debug.LogFormat("Missing row colors test A");
            return false;
        }
            if (allCols.Any(x => Enumerable.Range(0, 2).Any(y => x.Select(z => z = z % 2).Count(z => z == y) < 2)))
        {
            Debug.LogFormat("missing col colors test A");
            return false;
        }
            if (!colors) //If we aren't using 4 colors, we don't care about adjacency yet.
                return true;

            if (allRows.Any(row => Enumerable.Range(0, 4).Any(num => !row.Contains(num)))) //Checks if any row has a missing color;
        {
            Debug.LogFormat("missing row colors test B");
            return false;
        }
            if (allCols.Any(x => Enumerable.Range(0, 4).Any(y => !x.Contains(y))))
        {
            Debug.LogFormat("missing col colors test B");
                return false;
        }
            if (Enumerable.Range(0, 36).Any(x => GetAdjacents(x).Select(y => board[y]).Any(y => y == board[x])))
        {
            Debug.LogFormat("bad adjacency");
                return false;
        }
            return true;
    }
    int[] Row(int[] board, int index)
    {
        return Enumerable.Range(0, 36).Where(x => x / 6 == index).Select(x => board[x]).ToArray();
    }
    int[] Col(int[] board, int index)
    {
        return Enumerable.Range(0, 36).Where(x => x % 6 == index).Select(x => board[x]).ToArray();
    }

    int[] GetAdjacents(int index)
    {
        List<int> adjacents = new List<int>();
        if (index > 5) adjacents.Add(index - 6);
        if (index < 30) adjacents.Add(index + 6);
        if (index % 6 != 0) adjacents.Add(index - 1);
        if (index % 6 != 5) adjacents.Add(index + 1);
        return adjacents.ToArray();
    }


    void ButtonToggle(int pos)
    {
        buttons[pos].AddInteractionPunch(0.1f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, buttons[pos].transform);
        if (moduleSolved) return;

        grid[pos] = (grid[pos] + 1) % 5;
        buttons[pos].GetComponent<MeshRenderer>().material = materials[grid[pos]];
        if (ValidGrid(grid, true))
        {
            moduleSolved = true;
            GetComponent<KMBombModule>().HandlePass();
            resetButton.GetComponentInChildren<TextMesh>().text = "CORRECT!";
        }

    }

    IEnumerator ResetFlip()
    {
        resetButton.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, resetButton.transform);

        whiteText = !whiteText;
        float modifier = whiteText ? 1.5f : -1.5f;
        Predicate<float> test;
        if (whiteText)
            test = (x => x <= 1);
        else
            test = (x => x >= 0);
        while (test(flipProgress))
        {
            resetButton.GetComponent<MeshRenderer>().material.color = Color.Lerp(white, black, flipProgress);
            resetButton.GetComponentInChildren<TextMesh>().color = Color.Lerp(black, white, flipProgress);
            flipProgress += modifier * Time.deltaTime;
            yield return null;
        }
        if (moduleSolved) yield break;
    }



#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} A1 [toggle a square] | !{0} row 4 011001 [change an entire row] | !{0} col C 101001 [change an entire column] | !{0} solve 100101001011010110110100101001011010 [give a full solution] | !{0} reset";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string input)
    {
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }
}
