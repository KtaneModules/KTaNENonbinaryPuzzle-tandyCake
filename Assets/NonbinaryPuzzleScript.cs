using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NonbinaryPuzzle;
using UnityEngine;

public class NonbinaryPuzzleScript : MonoBehaviour
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
    int[] givens;
    int[] solution;

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
        solution = recurse(new int?[36]).First();

        givens = Ut.ReduceRequiredSet(Enumerable.Range(0, 36).ToArray().Shuffle(), test: state =>
        {
            var board = new int?[36];
            foreach (var cell in state.SetToTest)
                board[cell] = solution[cell];
            return recurse(board).Take(2).Count() == 1;
        }).ToArray();

        foreach (var given in givens)
            displayedGrid[given] = solution[given];
        DisplayGrid();
    }

    IEnumerable<int[]> recurse(int?[] board)
    {
        // Check if the whole board is done
        if (board.All(i => i != null))
        {
            yield return board.Select(i => i.Value).ToArray();
            yield break;
        }

        // Find an unfilled square that has the fewest possibilities
        var bestCell = -1;
        var fewestPossibilities = 5;
        int x, y;
        for (var cell = 0; cell < 36; cell++)
        {
            if (board[cell] != null)
                continue;
            x = cell % 6;
            y = cell / 6;
            var possibilities = Enumerable.Range(0, 4).Count(color => (x == 0 || board[cell - 1] != color) && (x == 5 || board[cell + 1] != color) && (y == 0 || board[cell - 6] != color) && (y == 5 || board[cell + 6] != color));
            if (possibilities < fewestPossibilities)
            {
                bestCell = cell;
                if (possibilities == 1)
                    goto shortcut;
                fewestPossibilities = possibilities;
            }
        }

        shortcut:
        x = bestCell % 6;
        y = bestCell / 6;
        var parityCounts = Enumerable.Range(0, 2).Select(parity => board.Count(cl => cl != null && cl.Value % 2 == parity)).ToArray();
        var offset = UnityEngine.Random.Range(0, 4);
        for (var colorIter = 0; colorIter < 4; colorIter++)
        {
            var color = (colorIter + offset) % 4;

            // Make sure not to place the same color next to itself
            if (!((x == 0 || board[bestCell - 1] != color) && (x == 5 || board[bestCell + 1] != color) && (y == 0 || board[bestCell - 6] != color) && (y == 5 || board[bestCell + 6] != color)))
                continue;
            // Make sure not to place more than 18 of each parity
            if (parityCounts[color % 2] >= 18)
                continue;

            board[bestCell] = color;

            // Make sure that placing this color hasn’t made it impossible to place the rest of the colors in the same ROW
            var numMissingColors = Enumerable.Range(0, 4).Count(clr => !Enumerable.Range(0, 6).Any(xx => board[xx + 6 * y] == clr));
            if (numMissingColors > Enumerable.Range(0, 6).Count(xx => board[xx + 6 * y] == null))
                continue;

            // Make sure that placing this color hasn’t made it impossible to place the rest of the colors in the same COLUMN
            numMissingColors = Enumerable.Range(0, 4).Count(clr => !Enumerable.Range(0, 6).Any(yy => board[x + 6 * yy] == clr));
            if (numMissingColors > Enumerable.Range(0, 6).Count(yy => board[x + 6 * yy] == null))
                continue;

            // All checks passed: try this color
            foreach (var solution in recurse(board))
                yield return solution;
        }
        board[bestCell] = null;
    }

    void DisplayGrid()
    {
        for (int i = 0; i < 36; i++)
            buttons[i].GetComponent<MeshRenderer>().sharedMaterial = displayedGrid[i] == null ? gray : materials[displayedGrid[i].Value];
    }

    void ButtonToggle(int pos)
    {
        buttons[pos].AddInteractionPunch(0.1f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, buttons[pos].transform);
        if (moduleSolved || givens.Contains(pos))
            return;

        if (displayedGrid[pos] == null)
            displayedGrid[pos] = 0;
        else
            displayedGrid[pos]++;
        if (displayedGrid[pos] > 3)
            displayedGrid[pos] = null;

        buttons[pos].GetComponent<MeshRenderer>().sharedMaterial = displayedGrid[pos] == null ? gray : materials[displayedGrid[pos].Value];
        if (Enumerable.Range(0, 36).All(ix => displayedGrid[ix] == solution[ix]))
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
            flipProgress += modifier * 1.5f * Time.deltaTime;
            yield return null;
        }
        if (moduleSolved)
            yield break;
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
