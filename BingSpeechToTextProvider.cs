using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.CognitiveServices.SpeechRecognition;
using System.Configuration;

using System.IO;

namespace SimpleEchoBot
{
    using System.Diagnostics;
    public class BingSpeech
    {
        private DataRecognitionClient dataClient;
        private Action<string> _callback;
        private Action<string> _failedCallback;

        public BingSpeech(Action<string> callback, Action<string> failedCallback)
        {
            _callback = callback;
            _failedCallback = failedCallback;
        }

        public string DefaultLocale { get; } = "en-US";

        public string SubsriptionKey { get; set; }

        public void CreateDataRecoClient()
        {
            this.SubsriptionKey = ConfigurationManager.AppSettings["MicrosoftSpeechApiKey"].ToString();
            this.dataClient = SpeechRecognitionServiceFactory.CreateDataClient(
                SpeechRecognitionMode.ShortPhrase,
                this.DefaultLocale,
                this.SubsriptionKey
                );
            this.dataClient.OnResponseReceived += this.OnResponseReceivedHandler;
            this.dataClient.OnConversationError += this.OnConversationError;
        }

        public void SendAudioHelper(Stream recordedStream)
        {
            int bytesRead = 0;
            byte[] buffer = new byte[1024];
            try
            {
                do
                {
                    bytesRead = recordedStream.Read(buffer, 0, buffer.Length);
                    this.dataClient.SendAudio(buffer, bytesRead);
                }
                while (bytesRead > 0);
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception --------------" + ex.Message);
            }
            finally
            {
                this.dataClient.EndAudio();
            }
        }

        private void OnResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            if(e.PhraseResponse.RecognitionStatus == RecognitionStatus.RecognitionSuccess)
            {
                string phraseResponse = e.PhraseResponse.Results.OrderBy(r => r.Confidence).FirstOrDefault().DisplayText;
                this._callback.Invoke(phraseResponse.ToString());
            }
        }

        private void OnConversationError(object sender, SpeechErrorEventArgs e)
        {
            Debug.WriteLine(e.SpeechErrorText);
            this._failedCallback.Invoke(e.SpeechErrorText);
        }
    }
}