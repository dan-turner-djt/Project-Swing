using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

public class TextBoxManager : MonoBehaviour
{
    public GeneralSceneController sc;
    public GeneralInput inp;
    public GameObject textBoxPrefab;
    TextMeshProUGUI displayText;
    TextMeshProUGUI speakerName;
    string fullText;

    public IEnumerator cTextSequence;
    public IEnumerator cAnimateText;

    public enum Character
    {
        Keiya, Monkey
    }

    public enum Emotion
    {
        Happy, Sad, Angry, Scared, Annoyed
    }

    bool animationComplete;

    const float defaultAnimateTextSpeed = 0.05f;
    const float fasterAnimateTextSpeed = 0.02f;
    float animateTextSpeed;

    bool holdingDownButton;

    List<TextSectionInfo> textSections = new List<TextSectionInfo>();
    int textSectionsCounter = 0;

    Dictionary<string, List<TextSectionInfo>> loadedText = new Dictionary<string, List<TextSectionInfo>>();


    private void Start()
    {
        sc = GameObject.FindGameObjectWithTag("SceneController").GetComponent<GeneralSceneController>();
        inp = GameObject.FindGameObjectWithTag("SceneController").GetComponent<GeneralInput>();
    }


    

    void ShowText(string toDisplay)
    {
        fullText = toDisplay;
        displayText.text = "";
        textSectionsCounter++;
        cAnimateText = AnimateText();
        StartCoroutine(cAnimateText);
    }

    public bool StartTextSequence (string sequenceName, bool controllable)
    {
        if (cTextSequence != null)
        {
            return false;
        }


        textSectionsCounter = 0;
        bool found = loadedText.TryGetValue(sequenceName, out textSections);

        if (!found)
        {
            Debug.Log("Dictionary does not contain that text sequence!");
            return false;
        }

        cTextSequence = TextSequence(controllable);
        StartCoroutine(cTextSequence);

        return true;
    }


    public bool LoadTextSequenceInformation (string sequenceName)
    {

        List<TextSectionInfo> newTextSections = new List<TextSectionInfo>();

        string path = "Assets/TextScripts/" + sequenceName + ".txt";
        string readString = "";

        try
        {
            StreamReader reader = new StreamReader(path);
            readString = reader.ReadToEnd();
        }
        catch (IOException)
        {
            Debug.Log("IO Exception");
            return false;
        }

        string[] sections = readString.Split('#');

        foreach (string s in sections)
        {
            if (s.Equals(""))
            {
                //the first one will begin with a slash and hence there will be an empty string at the start when split
                continue;
            }

            string[] parts = s.Split('$');

            if (parts.Length != 3)
            {
                continue;
            }

            string speakerName = parts[0];
            string emotion = parts[1];
            string textBody = parts[2];

            TextSectionInfo newInfo = new TextSectionInfo();
            newInfo.textBody = textBody;
            bool setSpeaker = newInfo.SetSpeaker(parts[0]);
            if (!setSpeaker) continue;
            bool setEmotion = newInfo.SetEmotion(parts[1]);
            if (!setEmotion) continue;
            newTextSections.Add(newInfo);
        }

        if (loadedText.ContainsKey (sequenceName))
        {
            Debug.Log("Dictionary already contains that text sequence!");
            return false;
        }

        loadedText.Add(sequenceName, newTextSections);
        

        return true;
    }


    IEnumerator TextSequence(bool controllable)
    {
        bool running = true;

        GameObject textBoxInstance = Instantiate(textBoxPrefab, GameObject.Find("Canvas").transform);
        displayText = GameObject.FindGameObjectWithTag("TextBoxDisplayText").GetComponent<TextMeshProUGUI>();
        speakerName = GameObject.FindGameObjectWithTag("TextBoxSpeakerName").GetComponent<TextMeshProUGUI>();

        fullText = "";
        displayText.text = fullText;
        speakerName.text = textSections[0].speaker.ToString();

        yield return new WaitForSecondsRealtime(0.5f);

        string toDisplay = textSections[0].textBody;
        ShowText(toDisplay);

        while (running)
        {
            

            if (animationComplete)
            {
                if (controllable)
                {
                    if (inp.GetPressed("Submit") && !holdingDownButton)
                    {
                        if (textSectionsCounter < textSections.Count)
                        {
                            toDisplay = textSections[textSectionsCounter].textBody;
                            speakerName.text = textSections[textSectionsCounter].speaker.ToString();
                            ShowText(toDisplay);
                        }
                        else
                        {
                            running = false;
                        }

                    }
                }
                else
                {
                    //allow time for reading
                    yield return new WaitForSecondsRealtime(1f);

                    if (textSectionsCounter < textSections.Count)
                    {
                       
                        toDisplay = textSections[textSectionsCounter].textBody;
                        speakerName.text = textSections[textSectionsCounter].speaker.ToString();
                        ShowText(toDisplay);
                    }
                    else
                    {
                        running = false;
                    }
                }
                
            }
            else
            {
                if (controllable)
                {
                    if (inp.GetPressed("Submit"))
                    {
                        animateTextSpeed = fasterAnimateTextSpeed;
                    }
                    else
                    {
                        animateTextSpeed = defaultAnimateTextSpeed;
                    }
                }
                else
                {
                    animateTextSpeed = fasterAnimateTextSpeed;
                }
                
            }


            //just so we can do a button down press check later

            if (inp.GetPressed("Submit"))
            {
                holdingDownButton = true;
            }
            else
            {
                holdingDownButton = false;
            }

            yield return new WaitForEndOfFrame();
        }

        Destroy(textBoxInstance);
        displayText = null;

        cTextSequence = null;
    }


    IEnumerator AnimateText()
    {
        animationComplete = false;

        foreach(char c in fullText)
        {
            displayText.text += c;
            yield return new WaitForSecondsRealtime(animateTextSpeed);
        }

        animationComplete = true;
    }


    public bool GetIsTextBoxActive()
    {
        if (cTextSequence == null)
        {
            return false;
        }

        return true;
    }


    public struct TextSectionInfo
    {
        public string textBody;
        public Character speaker;
        public Emotion emotion;

        public bool SetSpeaker (string _character)
        {
            switch (_character)
            {
                case "Keya":
                    speaker = Character.Keiya;
                    break;
                case "Keiya":
                    speaker = Character.Keiya;
                    break;
                case "Monkey":
                    speaker = Character.Monkey;
                    break;
                case "Saru":
                    speaker = Character.Monkey;
                    break;
                default:
                    Debug.Log("Unkown character name!");
                    return false;
            }

            return true;
        }

        public bool SetEmotion (string _emotion)
        {
            switch (_emotion)
            {
                case "Happy":
                    emotion = Emotion.Happy;
                    break;
                case "Sad":
                    emotion = Emotion.Sad;
                    break;
                case "Scared":
                    emotion = Emotion.Scared;
                    break;
                case "Angry":
                    emotion = Emotion.Angry;
                    break;
                case "Annoyed":
                    emotion = Emotion.Angry;
                    break;
                default:
                    Debug.Log("Unkown emotion name!");
                    return false;
            }

            return true;
        }
    }
}


