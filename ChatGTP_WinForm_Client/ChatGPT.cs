using System.Collections;
using System.Speech.Synthesis;
using System.Speech.Recognition;
using Newtonsoft.Json;
using System.Text;
using System.Globalization;

namespace ChatGTP_WinForm_Client;

public partial class ChatGPT : Form
{
    string OPENAI_API_KEY = string.Empty;
    SpeechRecognitionEngine? _speechRecognitionEngine = null;
    SpeechSynthesizer? _speechSynthesizer = null;
    HttpClient _client = new HttpClient();

    public ChatGPT()
    {
        InitializeComponent();
    }

    private void ChatGPT_Load(object sender, EventArgs e)
    {
        var secretAppsettingReader = new SecretAppsettingReader.SecretAppsettingReader();
        var chatGptApiKey = secretAppsettingReader.ReadSection<string>("ChatGPTApiKey", true, typeof(Program).Assembly);

        if (string.IsNullOrWhiteSpace(chatGptApiKey))
        {
            MessageBox.Show("Please enter your OpenAI API key in the Configuration.");
            Application.Exit();
        }
        else
        {
            OPENAI_API_KEY = chatGptApiKey;
        }

        _client.DefaultRequestHeaders.Add("authorization", $"Bearer {chatGptApiKey}");

        //SetModels();
        cbModel.SelectedIndex = 0;

        cbVoice.Items.Clear();
        SpeechSynthesizer synth = new SpeechSynthesizer();
        foreach (var voice in synth.GetInstalledVoices())
            cbVoice.Items.Add(voice.VoiceInfo.Name);
        cbVoice.SelectedIndex = 0;
    }

    private void chkListen_CheckedChanged(object sender, EventArgs e)
    {
        if (chkListen.Checked)
        {
            lblSpeech.Text = string.Empty;
            lblSpeech.Visible = true;
            SpeechToText();
        }
        else
        {
            _speechRecognitionEngine!.RecognizeAsyncStop();
            lblSpeech.Visible = false;
        }
    }

    private void chkMute_CheckedChanged(object sender, EventArgs e)
    {
        if (chkMute.Checked)
        {
            lblVoice.Visible = false;
            cbVoice.Visible = false;
        }
        else
        {
            lblVoice.Visible = true;
            cbVoice.Visible = true;
        }
    }

    private void SpeechToText()
    {
        if (_speechRecognitionEngine != null)
        {
            _speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
            return;
        }

        _speechRecognitionEngine = new SpeechRecognitionEngine(new CultureInfo("en-US"));
        _speechRecognitionEngine.LoadGrammar(new DictationGrammar());
        _speechRecognitionEngine.SpeechRecognized += OnSpeechRecognized!;
        _speechRecognitionEngine.SpeechHypothesized += OnSpeechHypothesized!;
        _speechRecognitionEngine.SetInputToDefaultAudioDevice();
        _speechRecognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
    }

    private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
    {
        lblSpeech.Text = string.Empty; // Reset Hypothesized text

        if (!txtQuestion.Text.Equals(string.Empty))
            txtQuestion.Text += "\n";

        string text = e.Result.Text;
        txtQuestion.Text += text;
    }

    private void OnSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
    {
        string text = e.Result.Text;
        lblSpeech.Text = text;
    }

    private void btnSend_Click(object sender, EventArgs e)
    { 
        string sQuestion = txtQuestion.Text;
        if (string.IsNullOrEmpty(sQuestion))
        {
            MessageBox.Show("Type in your question!");
            txtQuestion.Focus();
            return;
        }

        if (!txtAnswer.Text.Equals(string.Empty))
        {
            txtAnswer.AppendText("\r\n");
        }

        txtAnswer.AppendText("Me: " + sQuestion + "\r\n");
        txtQuestion.Text = string.Empty;

        try
        {
            string sAnswer = SendMsg(sQuestion);
            txtAnswer.AppendText("Chat GPT: " + sAnswer.Replace( "\n", "\r\n"));
            SpeechToText(sAnswer);
        }
        catch (Exception ex)
        {
            txtAnswer.AppendText("Error: " + ex.Message);
        }       
    }

    public void SpeechToText(string s)
    {
        if (chkMute.Checked)
            return;

        if (_speechSynthesizer == null)
        {
            _speechSynthesizer = new SpeechSynthesizer();
            _speechSynthesizer.SetOutputToDefaultAudioDevice();
        }

        if (!cbVoice.Text.Equals(string.Empty))
            _speechSynthesizer.SelectVoice(cbVoice.Text);

        _speechSynthesizer.Speak(s);
    }
    
    public string SendMsg(string sQuestion)
    {
        int iMaxTokens = int.Parse( txtMaxTokens.Text); // 2048

        double.TryParse(txtTemperature.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var dTemperature); // 0.5
        if (dTemperature < 0d | dTemperature > 1d)
        {
            MessageBox.Show("Randomness has to be between 0 and 1 with higher values resulting in more random text");
            return string.Empty;
        }

        string sUserId = txtUserID.Text; // 1
        string sModel = cbModel.Text; // text-davinci-002, text-davinci-003

        string data = "{";
        data += " \"model\":\"" + sModel + "\",";
        data += " \"prompt\": \"" + PadQuotes(sQuestion) + "\",";
        data += " \"max_tokens\": " + iMaxTokens + ",";
        data += " \"user\": \"" + sUserId + "\", ";
        data += " \"temperature\": " + dTemperature.ToString(CultureInfo.InvariantCulture) + ", ";
        data += " \"frequency_penalty\": 0.0" + ", "; // Number between -2.0 and 2.0  Positive value decrease the model's likelihood to repeat the same line verbatim.
        data += " \"presence_penalty\": 0.0" + ", "; // Number between -2.0 and 2.0. Positive values increase the model's likelihood to talk about new topics.
        data += " \"stop\": [\"#\", \";\"]"; // Up to 4 sequences where the API will stop generating further tokens. The returned text will not contain the stop sequence.
        data += "}";

        HttpResponseMessage response = _client
            .PostAsync("https://api.openai.com/v1/completions", new StringContent(data, Encoding.UTF8, "application/json")).GetAwaiter().GetResult();
        string responseString = response.Content.ReadAsStringAsync().Result;

        var dyData = JsonConvert.DeserializeObject<dynamic>(responseString);
        var sResponse = dyData!.choices[0].text;

        return sResponse!;
    }

    private string PadQuotes(string s)
    {
        if (s.Contains("\\"))
            s = s.Replace("\\", @"\\");
                
        if (s.Contains("\n\r"))
            s = s.Replace("\n\r", @"\n");

        if (s.Contains("\r"))
            s = s.Replace("\r", @"\r");

        if (s.Contains("\n"))
            s = s.Replace("\n", @"\n");

        if (s.Contains("\t"))
            s = s.Replace("\t", @"\t");
        
        if (s.Contains("\""))
            return s.Replace("\"", @"""");
        
        return s;
    }
    
    private void SetModels()
    {
        HttpResponseMessage response = _client.GetAsync("https://api.openai.com/v1/models").GetAwaiter().GetResult();
        string responseString = response.Content.ReadAsStringAsync().Result;

        cbModel.Items.Clear();

        var oSortedList = new SortedList();
        var dyData = JsonConvert.DeserializeObject<dynamic>(responseString);

        foreach(var data in dyData!.data)
        {
            var sId = (string)data.id;
            if (oSortedList.ContainsKey(sId) == false)
            {
                oSortedList.Add(sId, sId);
            }
        }

        foreach (DictionaryEntry oItem in oSortedList)
            cbModel.Items.Add(oItem.Key);
    }
}
