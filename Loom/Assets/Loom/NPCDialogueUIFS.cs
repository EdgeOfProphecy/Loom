/*

The MIT License (MIT)

Copyright (c) 2015 Secret Lab Pty. Ltd. and Yarn Spinner contributors.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Text;
using System.Collections.Generic;
using Yarn.Unity;
using MagnetHillCo;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;
using Yarn;
using TMPro;

// Displays dialogue lines to the player, and sends
// user choices back to the dialogue system.

// Note that this is just one way of presenting the
// dialogue to the user. The only hard requirement
// is that you provide the RunLine, RunOptions, RunCommand
// and DialogueComplete coroutines; what they do is up to you.

public class NPCDialogueUIFS : DialogueUIBehaviour
{
    // The object that contains the dialogue and the options.
    // This object will be enabled when conversation starts, and
    // disabled when it ends.
    public GameObject dialogueContainer;

    // The UI element that displays lines
    public TextMeshProUGUI lineText;

    // A UI element that appears after lines have finished appearing
    public GameObject continuePrompt;

    // A delegate (ie a function-stored-in-a-variable) that
    // we call to tell the dialogue system about what option
    // the user selected
    private Yarn.OptionChooser SetSelectedOption;

    [Tooltip("How quickly to show the text, in seconds per character")]
    public float[] textSpeeds;

    // The buttons that let the user choose an option
    public List<Button> optionButtons;

    private InputManager im;
    private EventSystem _eventSystem;

    private Regex inlineStyleRegex;
    private Regex tagOpenRegex;
    private Regex tagCloseRegex;

    private int _textSpeedIndex = 0;
    public bool blockSkip = false;

    DialogueRunner dr;

    void Awake()
    {
        // Start by hiding the container, line and option buttons
        if (dialogueContainer != null)
            dialogueContainer.SetActive(false);

        lineText.gameObject.SetActive(false);

        foreach (var button in optionButtons)
        {
            button.gameObject.SetActive(false);
        }

        // Hide the continue prompt if it exists
        if (continuePrompt != null)
            continuePrompt.SetActive(false);

        inlineStyleRegex = new Regex("{{([^{}]*)}}");
        tagOpenRegex = new Regex("<([^/>]*)>");
        tagCloseRegex = new Regex("<(/[^>]*)>");

        dr = MHC.FindGameObjectComponentWithTag<DialogueRunner>(FSTokens.TagDialogueSystemManager);
    }

    public int textSpeedIndex
    {
        get
        {
            return _textSpeedIndex;
        }
        set
        {
            _textSpeedIndex = value;
        }
    }

    void Start()
    {
        //im = MHC.FindGameObjectComponentInChildrenWithTag<InputManager>(FSTokens.TagInputManager);
        im = GetComponent<InputManager>();
    }

    private float textSpeed
    {
        get
        {
            return textSpeeds[_textSpeedIndex];
        }
    }

    private IEnumerator parseInlineCommand(string command, bool skipActive)
    {
        Debug.Log("Parsing an inline command: " + command);
        if (command.Length > 5 && command.Substring(0, 5) == "delay")
        {
            if (skipActive)
            {
                Debug.Log("Delay commands aren't executed during skips");
            }
            else
            {
                float delay = float.Parse(command.Substring(6, command.Length - 1 - 6));
                Debug.Log("Executing delay for: " + delay);
                yield return new WaitForSeconds(delay);
            }
        }
        else if (command.Length >= 7 && command.Substring(0, 7) == "no_skip")
        {
            //Block skipping for this line.
            Debug.Log("Executing skip block");
            blockSkip = true;
        }
        else
        {
            Debug.Log("Execute function: " + command);

            //Ok, I need to regexify this command to get the function name and the argument list for it.
            string[] splitCommand = Regex.Split(command, @"\((.*?)\)");
            Debug.Assert(splitCommand.Length > 1, "Failed to evaluate a function name and argument list for this YarnSpinner inline command: " + command);

            if(dr.dialogue.library.FunctionExists(splitCommand[0]))
            {
                Debug.Log("Function found: " + splitCommand[0]);
                Debug.Log("Building argument list and executing: " + splitCommand[1]);

                string[] splitArguments = Regex.Split(splitCommand[1], ",");
                Value[] args = new Value[splitArguments.Length];

                for (int i = 0; i < splitArguments.Length; ++i)
                {
                    args[i] = YarnUtilitiesFS.ValueFromString(splitArguments[i]);
                    //Debug.Log("Arg" + i + ": " + splitArguments[i]);
                }

                dr.dialogue.library.GetFunction(splitCommand[0]).Invoke(args);
            }
            else
            {
                Debug.Assert(true, "Yarn function couldn't be found by name: " + splitCommand[0] + " derived from inline command: " + command);
            }
        }
        yield return null;
    }

    private struct OpenStyleTag
    {
        public string openTag;
        public string closeTag;

        public OpenStyleTag(string openTag, string closeTag)
        {
            this.openTag = openTag;
            this.closeTag = closeTag;
        } 
    }

    // Show a line of dialogue, gradually
    public override IEnumerator RunLine(Yarn.Line line)
    {
        //We'll always start a line assuming it can't be skipped.
        blockSkip = false;
        // Show the text
        lineText.gameObject.SetActive(true);

        StringBuilder displayText = new StringBuilder();

        string rawText = line.text;
        string[] parsedTextChunks = inlineStyleRegex.Split(rawText);

        MatchCollection collection = inlineStyleRegex.Matches(rawText);
        string[] inlineCommandStrings = new string[collection.Count];
        Dictionary<int, List<string>> inlineCommandAndIndexPairs = new Dictionary<int, List<string>>();
        int index = 0;
        foreach (Match match in collection)
        {
            string trimmedString = match.Value.Trim(new char[] { '{', '}' });
            //We have an isolated command
            Debug.Log("Match val: " + match.Value);
            Debug.Log("Trimmed string: " + trimmedString);
            inlineCommandStrings[index] = trimmedString;
            ++index;
        }

        for (int i = 0; i < inlineCommandStrings.Length; ++i)
        {
            Debug.Log("Command String Bank " + i + ":" + inlineCommandStrings[i]);
        }

        Debug.Log("Split line into " + parsedTextChunks.Length + " chunks");
        for (int i = 0; i < parsedTextChunks.Length; ++i)
        {
            bool commandFound = false;
            for (int j = 0; j < inlineCommandStrings.Length; ++j)
            {
                //Debug.Log("Comparing chunk: " + parsedTextChunks[i] + " against command: " + inlineCommandStrings[j]);
                if (inlineCommandStrings[j] == parsedTextChunks[i])
                {
                    Debug.Log("Command found: " + inlineCommandStrings[j] + " at index: " + displayText.Length);
                    if (!inlineCommandAndIndexPairs.ContainsKey(displayText.Length))
                    {
                        inlineCommandAndIndexPairs[displayText.Length] = new List<string>();
                    }
                    inlineCommandAndIndexPairs[displayText.Length].Add(inlineCommandStrings[j]);
                    commandFound = true;
                    continue;
                }
            }
            //Guess it wasn't a command match
            if (!commandFound)
            {
                displayText.Append(parsedTextChunks[i]);
                Debug.Log("Text Chunk: " + parsedTextChunks[i]);
            }
        }

        //Now that we have commands extracted and their cursor positions tagged, we can go about building a catolog of inline style tags
        Dictionary<int, OpenStyleTag> openStyleTags = new Dictionary<int, OpenStyleTag>();
        MatchCollection tagOpenCollection = tagOpenRegex.Matches(displayText.ToString());
        foreach (Match match in tagOpenCollection)
        {
            //Ok, I need to generate a closed tag that can match the open tag.
            string generatedClosedTag = match.Value;
            //Strip out any parameters.
            generatedClosedTag = Regex.Replace(generatedClosedTag, @"\s[^>]*", "");
            generatedClosedTag = Regex.Replace(generatedClosedTag, "=[^>]*", "");
            //Add a /, and you're left with a generated closing tag.
            generatedClosedTag = generatedClosedTag.Insert(1, "/");

            openStyleTags[match.Index] = new OpenStyleTag(match.Value, generatedClosedTag);
            Debug.Log("Tag Open Match: " + match.Value);
            Debug.Log("Generated closed: " + generatedClosedTag);
        }

        Dictionary<int, string> closedStyleTags = new Dictionary<int, string>();
        MatchCollection tagCloseCollection = tagCloseRegex.Matches(displayText.ToString());
        foreach (Match match in tagCloseCollection)
        {
            closedStyleTags[match.Index] = match.Value;
            Debug.Log("Tag Closed Match: " + match.Value);
        }

        //Now we know when a tag will begin, will it will end, and the tag itself.
        //This will let us assemble our string correctly while we're parsing it char by char.
        //Of course, we can just 

        Debug.Log("---Parsing complete, starting text render---");
        if (textSpeed > 0.0f)
        {
            // Display the line one character at a time
            var stringBuilder = new StringBuilder();
            int currCharIndex = 0;

            var enumerator = displayText.ToString().GetEnumerator();
            int commandIndex = 0;
            int closeTagLength = 0;
            while (enumerator.MoveNext())
            {
                char c = enumerator.Current;

                //We've run across a point where a command should execute.
                if (inlineCommandAndIndexPairs.ContainsKey(currCharIndex))
                {
                    for (int i = 0; i < inlineCommandAndIndexPairs[currCharIndex].Count; ++i)
                    {
                        yield return parseInlineCommand(inlineCommandAndIndexPairs[currCharIndex][i], false);
                    }
                    ++commandIndex;
                }

                if ((im.GetButtonDown(FSTokens.InputUIAccept) == true || im.GetButtonDown(FSTokens.InputUICancel) == true) && !blockSkip)
                {
                    Debug.Log("Skip requested! " + blockSkip);
                    //If we skip to the end of the dailog, just go ahead and execute all outstanding commands.
                    for (int i = commandIndex; i < inlineCommandStrings.Length; ++i)
                    {
                        yield return parseInlineCommand(inlineCommandStrings[i], true);
                    }
                    lineText.text = displayText.ToString();
                    yield return new WaitForSeconds(textSpeed);
                    break;
                }

                while (openStyleTags.ContainsKey(currCharIndex))
                {
                    //Add the start tag
                    buildDisplayString(stringBuilder, openStyleTags[currCharIndex].openTag, closeTagLength);
                    Debug.Log("Adding open tag: " + openStyleTags[currCharIndex].openTag);
                    //Add the ending tag
                    buildDisplayString(stringBuilder, openStyleTags[currCharIndex].closeTag, closeTagLength);
                    Debug.Log("Adding closed tag: " + openStyleTags[currCharIndex].closeTag);
                    closeTagLength += openStyleTags[currCharIndex].closeTag.Length;

                    Debug.Log("Close tag length :" + closeTagLength);
                    //Advance the iterator and char count past the start tag.
                    int oldCharIndex = currCharIndex;
                    for (int i = 0; i < openStyleTags[oldCharIndex].openTag.Length; ++i)
                    {
                        ++currCharIndex;
                        enumerator.MoveNext();
                    }
                    c = enumerator.Current;
                }
                while (closedStyleTags.ContainsKey(currCharIndex))
                {
                    //Skip ahead past the end of the closing tag. There's no need to add anything to the string, since the closing tag has been there the whole time.
                    Debug.Log("Closed tag found, skipping past it");
                    int oldCharIndex = currCharIndex;
                    closeTagLength -= closedStyleTags[oldCharIndex].Length;
                    Debug.Log("Close tag length :" + closeTagLength);
                    for (int i = 0; i < closedStyleTags[oldCharIndex].Length; ++i)
                    {
                        ++currCharIndex;
                        enumerator.MoveNext();
                    }
                    //We're done with this tag. Next time we encounter one it'll be a new pair.
                    if (currCharIndex <= displayText.Length - 1)
                    {
                        c = enumerator.Current;
                    }
                }

                if (currCharIndex <= displayText.Length - 1)
                {
                    buildDisplayString(stringBuilder, c, closeTagLength);
                    ++currCharIndex;
                }

                lineText.text = stringBuilder.ToString();
                yield return new WaitForSeconds(textSpeed);
            }
        }
        else
        {
            // Display the line immediately if textSpeed == 0
            lineText.text = displayText.ToString();
            for (int i = 0; i < inlineCommandStrings.Length; ++i)
            {
                //Execute all of the inline commands
                yield return parseInlineCommand(inlineCommandStrings[i], true);
            }
        }

        // Show the 'press any key' prompt when done, if we have one
        if (continuePrompt != null)
            continuePrompt.SetActive(true);

        // Wait for any user input
        while (im.GetButtonDown(FSTokens.InputUIAccept) == false && im.GetButtonDown(FSTokens.InputUICancel) == false)
        {
            yield return null;
        }

        // Hide the text and prompt
        lineText.gameObject.SetActive(false);

        if (continuePrompt != null)
            continuePrompt.SetActive(false);
    }

    private void buildDisplayString(StringBuilder stringBuilder, string s, int closeTagLength)
    {
        if (closeTagLength == 0)
        {
            stringBuilder.Append(s);
        }
        else
        {
            stringBuilder.Insert(stringBuilder.Length - (closeTagLength), s);
        }
    }

    private void buildDisplayString(StringBuilder stringBuilder, char c, int closeTagLength)
    {
        if (closeTagLength == 0)
        {
            stringBuilder.Append(c);
        }
        else
        {
            stringBuilder.Insert(stringBuilder.Length - (closeTagLength), c);
        }
    }

    // Show a list of options, and wait for the player to make a selection.
    public override IEnumerator RunOptions(Yarn.Options optionsCollection,
                                            Yarn.OptionChooser optionChooser)
    {
        // Do a little bit of safety checking
        if (optionsCollection.options.Count > optionButtons.Count)
        {
            Debug.LogWarning("There are more options to present than there are" +
                                "buttons to present them in. This will cause problems.");
        }

        // Display each option in a button, and make it visible
        int i = 0;
        foreach (var optionString in optionsCollection.options)
        {
            optionButtons[i].gameObject.SetActive(true);
            optionButtons[i].GetComponentInChildren<Text>().text = optionString;
            i++;
        }

        // Record that we're using it
        SetSelectedOption = optionChooser;
        if (optionsCollection.options.Count > 0)
        {
            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(optionButtons[0].gameObject);
        }

        // Wait until the chooser has been used and then removed (see SetOption below)
        while (SetSelectedOption != null)
        {
            yield return null;
        }

        // Hide all the buttons
        foreach (var button in optionButtons)
        {
            button.gameObject.SetActive(false);
        }
    }

    // Called by buttons to make a selection.
    public void SetOption(int selectedOption)
    {

        // Call the delegate to tell the dialogue system that we've
        // selected an option.
        SetSelectedOption(selectedOption);

        // Now remove the delegate so that the loop in RunOptions will exit
        SetSelectedOption = null;
    }

    // Run an internal command.
    public override IEnumerator RunCommand(Yarn.Command command)
    {
        // "Perform" the command
        Debug.Log("Command: " + command.text);

        yield break;
    }

    public override IEnumerator DialogueStarted()
    {
        Debug.Log("Dialogue starting!");
        NotificationCenter.DefaultCenter.Post(FSTokens.NotifDialogueStarted);

        // Enable the dialogue controls.
        if (dialogueContainer != null)
        {
            InputManager.PushInputManagerOntoStack(im);
            dialogueContainer.SetActive(true);
        }

        yield break;
    }

    // Yay we're done. Called when the dialogue system has finished running.
    public override IEnumerator DialogueComplete()
    {
        Debug.Log("Complete!");
        NotificationCenter.DefaultCenter.Post(FSTokens.NotifDialogueComplete);

        // Hide the dialogue interface.
        if (dialogueContainer != null)
        {
            dialogueContainer.SetActive(false);
            InputManager.PopInputManagerOffStack();
        }

        yield break;
    }

    private EventSystem eventSystem
    {
        get
        {
            if (!_eventSystem)
            {
                _eventSystem = MagnetHillCo.MHC.FindGameObjectComponentWithTag<EventSystem>(FSTokens.TagUIEventSystem);
            }
            return _eventSystem;
        }
    }
}
