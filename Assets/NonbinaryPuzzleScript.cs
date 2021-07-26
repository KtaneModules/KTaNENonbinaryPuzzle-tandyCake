using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NonbinaryPuzzle;
using UnityEngine;
using System.Text.RegularExpressions;

public class NonbinaryPuzzleScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMColorblindMode Colorblind;

    public KMSelectable resetButton;
    public KMSelectable[] buttons;
    public Material[] materials;
    public Material gray;
    public TextMesh ypCounter, wkCounter;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    bool cbON;

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
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { ButtonToggle(Array.IndexOf(buttons, button)); return false; };
        resetButton.OnInteract += delegate ()
        {
            if (isFlipping != null) StopCoroutine(isFlipping);
            isFlipping = StartCoroutine(ResetFlip());
            return false;
        };
        GetComponent<KMBombModule>().OnActivate += delegate ()
        {
            if (Colorblind.ColorblindModeActive)
                ToggleCB();
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

        ypCounter.color = UnityEngine.Random.Range(0, 2) == 0 ? "FFFBBC".Color() : "EBCBEE".Color();
        wkCounter.color = UnityEngine.Random.Range(0, 2) == 0 ? Color.white : "ADADAD".Color();

        DisplayGrid();
        DoLogging();
    }
    

    void DoLogging()
    {
        Debug.LogFormat("[Nonbinary Puzzle #{0}] The displayed grid is as follows:", moduleId);
        LogGrid(displayedGrid.Select(x => x ?? -1).ToArray(), 6, 6, ".YWPK", 1);
        Debug.LogFormat("[Nonbinary Puzzle #{0}] ", moduleId);
        Debug.LogFormat("[Nonbinary Puzzle #{0}] The solution is as follows:", moduleId);
        LogGrid(solution, 6, 6, "YWPK", 0);
    }

    //Puzzle generation code by Timwi.
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
            displayedGrid[i] = null;
        foreach (var given in givens)
            displayedGrid[given] = solution[given];
        for (int i = 0; i < 36; i++)
        {
            buttons[i].GetComponent<MeshRenderer>().sharedMaterial = displayedGrid[i] == null ? gray : materials[displayedGrid[i].Value];
            DisplayCB(i);
        }
        SetDisplays();
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
        SetDisplays();
        DisplayCB(pos);
        if (Enumerable.Range(0, 36).All(ix => displayedGrid[ix] == solution[ix]))
        {
            moduleSolved = true;
            GetComponent<KMBombModule>().HandlePass();
            resetButton.GetComponentInChildren<TextMesh>().text = "CORRECT!";
        }
    }

    void LogGrid(int[] grid, int height, int length, string charSet, int shift = 0)
    {
        string logger = string.Empty;
        for (int i = 0; i < height * length; i++)
        {
            logger += charSet[grid[i] + shift];
            if (i % length == length - 1)
            {
                Debug.LogFormat("[Nonbinary Puzzle #{0}] {1}", moduleId, logger);
                logger = string.Empty;
            }
        }
    }

    void ToggleCB()
    {
        cbON = !cbON;
        for (int i = 0; i < 36; i++)
            DisplayCB(i);
    }
    void DisplayCB(int pos)
    {
        buttons[pos].GetComponentInChildren<TextMesh>().text =
            (cbON && displayedGrid[pos] != null) ?
            new string[] { "Y", string.Empty, "P", string.Empty }[(int)displayedGrid[pos]] :
            string.Empty;
    }
    void SetDisplays()
    {
        ypCounter.text = displayedGrid.Count(x => x % 2 == 0).ToString();
        wkCounter.text = displayedGrid.Count(x => x % 2 == 1).ToString();
    }

    IEnumerator ResetFlip(float speed = 2.25f)
    {
        resetButton.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, resetButton.transform);
        if (!moduleSolved)
           DisplayGrid();

        whiteText = !whiteText;
        float modifier = whiteText ? 1 : -1;
        Predicate<float> test;
        if (whiteText)
            test = (x => x <= 1);
        else
            test = (x => x >= 0);
        while (test(flipProgress))
        {
            resetButton.GetComponent<MeshRenderer>().material.color = Color.Lerp(white, black, flipProgress);
            resetButton.GetComponentInChildren<TextMesh>().color = Color.Lerp(black, white, flipProgress);
            flipProgress += modifier * speed * Time.deltaTime;
            yield return null;
        }
        if (moduleSolved)
            yield break;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "[!{0} A4 B3 F5] toggles those cells. [!{0} A4 Y B3 K] sets those cells to those colors. [!{0} row 4 YWPKYW] sets that row from left to right. [!{0} col D YWPKYW] sets that column from top to bottom. [!{0} solve YWPK...] to enter that whole grid. [!{0} clear A4 A5] sets those squares to gray (if applicable). [!{0} reset] presses the reset button. [!{0} colorblind] toggles colorblind mode.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] coords = Enumerable.Range(0, 36).Select(x => "ABCDEF"[x % 6].ToString() + "123456"[x / 6]).ToArray(); //This is dumb.
        string[] letters = { "Y", "W", "P", "K" };
        command = command.Trim().ToUpperInvariant();
        List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parameters.All(x => coords.Contains(x)))
        {
            yield return null;
            foreach (string coord in parameters)
            {
                buttons[Array.IndexOf(coords, coord)].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        else if (parameters.Where((_, pos) => pos % 2 == 0).All(x => coords.Contains(x)) && //If every odd position is a coordinate
                parameters.Where((_, pos) => pos % 2 == 1).All(x => letters.Contains(x)) && //And every odd position is a color
                parameters.Count % 2 == 0) //And there's an even number of terms
        {
            yield return null;
            for (int i = 0; i < parameters.Count; i += 2)
            {
                int buttonIx = Array.IndexOf(coords, parameters[i]);
                while (displayedGrid[buttonIx] != "YWPK".IndexOf(parameters[i + 1][0]) && !givens.Contains(buttonIx)) //Ignore the command if the chosen cell is given.
                {
                    buttons[buttonIx].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        else if (Regex.IsMatch(command, @"^(ROW\s+[1-6])|(COL(UMN)?\s+[A-F])\s+[YWPK]{6}$"))
        {
            int rcIx = "ABCDEF123456".IndexOf(parameters[1][0]) % 6;
            int[] buttonVals = parameters.Last().Select(x => "YWPK".IndexOf(x)).ToArray();
            int[] buttonPlaces = parameters.First() == "ROW" ?
                  Enumerable.Range(0, 6).Select(x => 6 * rcIx + x).ToArray() :
                  Enumerable.Range(0, 6).Select(x => 6 * x + rcIx).ToArray();
            yield return null;
            for (int i = 0; i < 6; i++)
            {
                while (displayedGrid[buttonPlaces[i]] != buttonVals[i] && !givens.Contains(buttonPlaces[i]))
                {
                    buttons[buttonPlaces[i]].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        else if (Regex.IsMatch(command, @"^SOLVE\s+[YWPK]{36}$"))
        {
            yield return null;
            for (int i = 0; i < 36; i++)
            {
                while (displayedGrid[i] != "YWPK".IndexOf(parameters.Last()[i]) && !givens.Contains(i))
                {
                    buttons[i].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        else if (parameters.First() == "CLEAR" || parameters.First() == "EMPTY")
        {
            parameters.RemoveAt(0);
            if (parameters.All(x => coords.Contains(x)))
            {
                yield return null;
                foreach (string coord in parameters)
                {
                    int ix = Array.IndexOf(coords, coord);
                    while (displayedGrid[ix] != null && !givens.Contains(ix))
                    {
                        buttons[ix].OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
        }
        else if (command == "RESET")
        {
            yield return null;
            resetButton.OnInteract();
        }
        else if(command.EqualsAny("COLORBLIND", "COLOURBLIND", "CB", "COLOR-BLIND", "COLOUR-BLIND"))
        {
            yield return null;
            ToggleCB();
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = 0; i < 36; i++)
            while (displayedGrid[i] != solution[i])
            {
                buttons[i].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
    }
}
