using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NonbinaryPuzzle;
using UnityEngine;


public class NonbinaryPuzzleScript: MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;

    public KMSelectable resetButton;
    public KMSelectable[] buttons;
    public Material[] materials;
    public Material gray;

    private bool moduleSolved;
    bool whiteText; //Controls reset button flipping
    float flipProgress;
    Coroutine isFlipping;
    private Color black = new Color(0.211f, 0.211f, 0.211f);
    private Color white = new Color(1, 1, 1);

    int?[] displayedGrid = new int?[36];
    int[] givens = new int[36].Select(x => x = 0).ToArray();
    int[] solution = new int[36];
    
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
        solution = GeneratePuzzle();
        DisplayGrid(solution.Cast<int?>().ToArray());//Debug
        
        /*var puzzleIxs = Ut.ReduceRequiredSet(Enumerable.Range(0, 36).ToArray().Shuffle(), test =>
        {
            for (int i = 0; i < 36; i++)
                displayedGrid[i] = null;
            foreach (int i in test.SetToTest)
                displayedGrid[i] = solution[i];
            return generatePuzzle(solution.ToArray(), _given, 0).Take(2).Count() == 1;
        });*/
    }


    int[] GeneratePuzzle()
    {
        Restart:
        List<int>[] valids = new List<int>[36];
        int[] grid;
        grid = new int[36].Select(x => x = -1).ToArray();
        List<int> availablePositions = Enumerable.Range(0, 36).ToList();
        Stack<int[]> saveStates = new Stack<int[]>();
        saveStates.Push(grid.ToArray());
        for (int colorIndex = 0; colorIndex < 4; colorIndex++)
        {
            PlaceThisColor:
            List<int> availableCols = Enumerable.Range(0, 6).ToList();
            for (int row = 0; row < 6; row++)
            {
                List<int> availableSpots = availableCols.Where(x => grid[6 * row + x] == -1).ToList();
                if (availableSpots.Count == 0)
                {
                    grid = saveStates.Pop();
                    row = 0;
                    goto PlaceThisColor;
                }
                int chosenCol = availableSpots.PickRandom();
                availableCols.Remove(chosenCol);
                grid[6 * row + chosenCol] = colorIndex;
            }
            saveStates.Push(grid.ToArray());
        }

        valids = GetValids(grid);
        if (Enumerable.Range(0, 36).Any(x => grid[x] == -1 && valids[x].Count == 0))
            goto Restart;
        while (valids.Any(x => x.Count == 1))
        {
            for (int i = 0; i < 36; i++)
            {
                if (valids[i].Count == 1)
                {
                    grid[i] = valids[i].First();
                    valids = GetValids(grid);
                    break;
                }
            }
        }
        while (Enumerable.Range(0,36).Any(i => grid[i] == -1 && (valids[i].All(x => x % 2 == 0) || valids[i].All(x => x % 2 == 1))))
        {
            if (Enumerable.Range(0, 36).Any(x => grid[x] == -1 && valids[x].Count == 0))
                goto Restart;
            for (int i = 0; i < 36; i++)
            {
                if (grid[i] == -1 && (valids[i].All(x => x % 2 == 0) || valids[i].All(x => x % 2 == 1)))
                {
                    grid[i] = valids[i].PickRandom();
                    valids = GetValids(grid);
                    break;
                }
            }
        }
        valids = GetValids(grid);
        int[] counts = new int[] { 18 - grid.Count(x => x % 2 == 0), 18 - grid.Count(x => x % 2 == 1) };
        if (counts.Any(x => x < 0))
            goto Restart;
        int[] parityOrders = Enumerable.Repeat(0, counts[0]).Concat(Enumerable.Repeat(1, counts[1])).ToArray().Shuffle();
        int pointer = 0;
        for (int i = 0; i < 36; i++)
        {
            if (grid[i] == -1)
            {
                grid[i] = valids[i].Where(x => x % 2 == parityOrders[pointer]).PickRandom();
                pointer++;
            }
        }
        if (!ValidGrid(grid))
            goto Restart;
        return grid;
    }
    List<int>[] GetValids(int[] grid)
    {
        List<int>[] valids = new List<int>[36];
        for (int i = 0; i < 36; i++)
        {
            if (grid[i] == -1)
                valids[i] = Enumerable.Range(0, 4).Where(num => !GetAdjacents(i).Select(x => grid[x]).Contains(num)).ToList();
            else valids[i] = new List<int>();
        }
        return valids;
    }
    void DisplayGrid(int?[] grid)
    {
        for (int i = 0; i < 36; i++)
        {
            displayedGrid[i] = grid[i];
            buttons[i].GetComponent<MeshRenderer>().material =
                (grid[i] == null || grid[i] == -1) ?
                gray :
                materials[(int)grid[i]];
        }
    }

    bool ValidGrid(int[] board)
    {
        if (board.Count(x => x % 2 == 0) != board.Count(x => x % 2 == 1))
            return false;
        IEnumerable<int[]> allRows = Enumerable.Range(0,6).Select(x => Row(board, x));
        IEnumerable<int[]> allCols = Enumerable.Range(0,6).Select(x => Col(board, x));
        if (allRows.Any(row => Enumerable.Range(0, 4).Any(x => !row.Contains(x))))
            return false;
        if (allCols.Any(col => Enumerable.Range(0, 4).Any(x => !col.Contains(x))))
            return false;
        if (Enumerable.Range(0, 36).Any(square => GetAdjacents(square).Select(x => board[x]).Contains(board[square])))
            return false;
        return true; //                                                                                                     :)
    }
    int[] Row(int[] board, int index)
    {
        return Enumerable.Range(0, 36).Where(x => x / 6 == index).Select(x => board[x]).ToArray();
    }
    int[] Col(int[] board, int index)
    {
        return Enumerable.Range(0, 36).Where(x => x % 6 == index).Select(x => board[x]).ToArray();
    }

    List<int> GetAdjacents(int index)
    {
        List<int> adjacents = new List<int>();
        if (index > 5) adjacents.Add(index - 6);
        if (index < 30) adjacents.Add(index + 6);
        if (index % 6 != 0) adjacents.Add(index - 1);
        if (index % 6 != 5) adjacents.Add(index + 1);
        return adjacents;
    }


    void ButtonToggle(int pos)
    {
        buttons[pos].AddInteractionPunch(0.1f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, buttons[pos].transform);
        if (moduleSolved) return;
        
        if (displayedGrid[pos] == null)
            displayedGrid[pos] = 0;
        else displayedGrid[pos]++;
        if (displayedGrid[pos] > 3)
            displayedGrid[pos] = null;
        
        buttons[pos].GetComponent<MeshRenderer>().material = (displayedGrid[pos] == null) ? gray : materials[(int)displayedGrid[pos]];
        if (displayedGrid.SequenceEqual(solution.Cast<int?>()))
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
            flipProgress += modifier * 1.5f*Time.deltaTime;
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
