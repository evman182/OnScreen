using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DisplaySwitcher
{
    public class VizioController
    {
        private readonly string _tvAuth;
        private readonly string _tvIpAddress;
        private readonly HttpClient _httpClient = new HttpClient();
        private const int MaxNumberOfTries = 3;

        public VizioController(string tvAuth, string tvIpAddress)
        {
            _tvAuth = tvAuth;
            _tvIpAddress = tvIpAddress;
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
            
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;
            
        }

        public void TurnOnTv()
        {
            var tries = 0;
            while (tries < MaxNumberOfTries)
            {
                try
                {

                    var requestBody = new StringContent("{\"KEYLIST\": [{\"CODESET\": 11,\"CODE\": 1,\"ACTION\":\"KEYPRESS\"}]}");
                    requestBody.Headers.Clear();
                    requestBody.Headers.Add("Content-Type", "application/json");
                    requestBody.Headers.Add("AUTH", _tvAuth);
                    var respones = _httpClient.PutAsync($"https://{_tvIpAddress}:9000/key_command/", requestBody).Result;
                    var responseText = respones.Content.ReadAsStringAsync().Result;
                    return;
                }
                catch (Exception)
                {
                    tries++;
                }
            }
            throw new Exception("Couldn't turn on tv");
        }

        public void TurnOffTv()
        {
            var tries = 0;
            while (tries < MaxNumberOfTries)
            {
                try
                {

                    var requestBody = new StringContent("{\"KEYLIST\": [{\"CODESET\": 11,\"CODE\": 0,\"ACTION\":\"KEYPRESS\"}]}");
                    requestBody.Headers.Clear();
                    requestBody.Headers.Add("Content-Type", "application/json");
                    requestBody.Headers.Add("AUTH", _tvAuth);
                    var respones = _httpClient.PutAsync($"https://{_tvIpAddress}:9000/key_command/", requestBody).Result;
                    var responseText = respones.Content.ReadAsStringAsync().Result;
                    return;
                }
                catch (Exception)
                {
                    Thread.Sleep(250);
                    tries++;
                }
            }
            throw new Exception("Couldn't turn off tv");
        }

        public void SetInput(string input)
        {
            var hash = GetCurrentHash();

            var tries = 0;
            while (tries < MaxNumberOfTries)
            {
                try
                {
                    var requestBody = new StringContent("{" + $"\"REQUEST\": \"MODIFY\",\"VALUE\": \"{input}\",\"HASHVAL\": {hash}" + "}");
                    requestBody.Headers.Clear();
                    requestBody.Headers.Add("Content-Type", "application/json");
                    requestBody.Headers.Add("AUTH", _tvAuth);
                    var respones = _httpClient.PutAsync($"https://{_tvIpAddress}:9000/menu_native/dynamic/tv_settings/devices/current_input",requestBody).Result;
                    var responseText = respones.Content.ReadAsStringAsync().Result;
                    return;
                }
                catch (Exception)
                {
                    Thread.Sleep(250);
                    tries++;
                }
            }
            throw new Exception("Couldn't set tv input");


        }

        private string GetCurrentHash()
        {
            var tries = 0;
            while (tries < MaxNumberOfTries)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, $"https://{_tvIpAddress}:9000/menu_native/dynamic/tv_settings/devices/current_input");
                    request.Headers.Clear();
                    request.Headers.Add("AUTH", _tvAuth);
                    var response = _httpClient.SendAsync(request).Result;
                    var responseText = response.Content.ReadAsStringAsync().Result;
                    var jsonObject = JObject.Parse(responseText);
                    var hash = jsonObject["ITEMS"][0]["HASHVAL"].Value<string>();
                    return hash;
                }
                catch (Exception)
                {
                    Thread.Sleep(250);
                    tries++;
                }
            }
            throw new Exception("Couldn't get hash");
        }
    }
}