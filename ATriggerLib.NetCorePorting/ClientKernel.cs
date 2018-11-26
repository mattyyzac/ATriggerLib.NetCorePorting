using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace ATriggerLib
{
    public class ClientKernel
    {
        private bool _isInited = false;
        private string _key;
        private string _secret;
        private string _apiServer;
        private int _apiTimeout;
        private bool _debug;
        private bool _async;
        private int _errors = 0;

        public ClientKernel(string key, string secret, bool Async, bool Debug, int APITimeout, string APIServer)
        {
            _isInited = true;
            _key = key;
            _secret = secret;
            _apiServer = APIServer;
            _apiTimeout = APITimeout;
            _debug = Debug;
            _async = Async;
            _errors = 0;
        }

        public void DoCreate(string _timeQuantity, string _timeValue, string _url, Dictionary<string, string> _tags,
            string _first = "", int _count = 1, int _retries = -1, Dictionary<string, string> _postData = null)
        {
            try
            {
                if (!_isInited)
                {
                    throw new Exception("Please initialize ATrigger first before using.");
                }

                var @in = _timeValue + _timeQuantity;
                @in = MakeURLready(@in);
                _first = MakeURLready(ToISO8601(_first));
                _url = MakeURLready(_url);
                var text = string.Empty;
                if (_tags != null)
                {
                    text = Dictionary2string(_tags, "tag_");
                }
                var urlQueries = $"timeSlice={@in}&url={_url}&first={_first}&count={_count}&retries={_retries}&{text}";
                CallATrigger("tasks/create", urlQueries, _postData);
            }
            catch (Exception ex)
            {
                _errors++;
                if (_debug)
                {
                    throw new Exception("A Trigger, DoCreate: " + ex.Message);
                }
            }
        }

        public string DoDelete(Dictionary<string, string> _tags)
        {
            return ActionUsingTags("tasks/delete", _tags);
        }

        public string DoPause(Dictionary<string, string> _tags)
        {
            return ActionUsingTags("tasks/pause", _tags);
        }

        public string DoResume(Dictionary<string, string> _tags)
        {
            return ActionUsingTags("tasks/resume", _tags);
        }

        public string DoGet(Dictionary<string, string> _tags)
        {
            return ActionUsingTags("tasks/get", _tags);
        }

        public int ErrorsCount()
        {
            return _errors;
        }

        public bool VerifyRequest(string requestIP)
        {
            var result = string.Empty;
            try
            {
                result = HttpRequest("ipverify", $"ip={MakeURLready(requestIP)}", null);
                if (!result.ToLower().Contains("\"ok\""))
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("\"error\"") && ex.Message.ToLower().Contains("\"invalid ip address"))
                {
                    return false;
                }
                if (_debug)
                {
                    throw new Exception(ex.Message.ToLower());
                }
            }
            return false;
        }

        private string ActionUsingTags(string urlType, Dictionary<string, string> _tags)
        {
            if (!_isInited)
            {
                throw new Exception("Please initialize ATrigger first before using.");
            }
            try
            {
                string urlQueries = Dictionary2string(_tags, "tag_");
                return CallATrigger(urlType, urlQueries, null);
            }
            catch (Exception ex)
            {
                _errors++;
                if (_debug)
                {
                    throw new Exception("A Trigger, ActionUsingTags: " + ex.Message);
                }
            }
            return string.Empty;
        }

        private string Dictionary2string(Dictionary<string, string> _in, string preKeyName = "")
        {
            var text = string.Empty;
            foreach (KeyValuePair<string, string> item in _in)
            {
                var text2 = text;
                text = text2 + preKeyName + Uri.EscapeDataString(item.Key) + "=" + Uri.EscapeDataString(item.Value) + "&";
            }
            if (text.EndsWith("&"))
            {
                text = text.Remove(text.Length - 1);
            }
            return text;
        }

        private string ToISO8601(string inDate)
        {
            if (inDate.Contains(" ") || inDate.Contains("/"))
            {
                inDate = DateTime.Parse(inDate).ToString("o");
            }
            return inDate;
        }

        private string MakeURLready(string _in)
        {
            return Uri.EscapeDataString(_in);
        }

        private string CallATrigger(string urlType, string urlQueries, Dictionary<string, string> postData)
        {
            if (!_async)
            {
                return HttpRequest(urlType, urlQueries, postData);
            }
            ThreadStart start = delegate
            {
                HttpRequest(urlType, urlQueries, postData);
            };
            var thread = new Thread(start);
            thread.Start();

            return "No response in Async version.";
        }

        private string HttpRequest(string urlType, string urlQueries, Dictionary<string, string> postData)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(_apiTimeout);

                    var requestUrl = $"{_apiServer}{urlType}?key={MakeURLready(_key)}&secret={MakeURLready(_secret)}&{urlQueries}";
                    FormUrlEncodedContent content = null;
                    if (postData != null)
                    {
                        content = new FormUrlEncodedContent(postData);
                    }

                    var res = client.PostAsync(requestUrl, content).Result;
                    if (res.IsSuccessStatusCode)
                    {
                        // 200 OK
                        var text = res.Content.ReadAsStringAsync().Result;
                        if (!text.Contains("\"ERROR\""))
                        {
                            return text;
                        }
                        _errors++;
                        if (_debug)
                        {
                            throw new Exception("ATrigger, Error Found In Result: " + text);
                        }
                    }
                    else
                    {
                        // some errors
                        var err = $"Status Code {res.StatusCode}. ";
                        err += res.Content.ReadAsStringAsync().Result;

                        _errors++;
                        if (_debug)
                        {
                            throw new Exception("ATrigger, Unexpected Status Code: " + err);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _errors++;
                if (_debug)
                {
                    throw new Exception("ATrigger, Unexpected Error: " + ex.Message);
                }
            }
            return string.Empty;
        }

        #region useless for now

        private string HttpRequest_NOUSENOW(string urlType, string urlQueries, Dictionary<string, string> postData)
        {
            try
            {
                Uri requestUri = new Uri(_apiServer + urlType + "?key=" + MakeURLready(_key) + "&secret=" + MakeURLready(_secret) + "&" + urlQueries);
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUri);
                httpWebRequest.Timeout = _apiTimeout;
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                httpWebRequest.ServicePoint.Expect100Continue = false;
                httpWebRequest.AllowWriteStreamBuffering = true;
                if (postData != null)
                {
                    httpWebRequest.Method = "POST";
                    string value = Dictionary2string(postData, "");
                    using (Stream stream = httpWebRequest.GetRequestStream())
                    {
                        using (StreamWriter streamWriter = new StreamWriter(stream))
                        {
                            streamWriter.Write(value);
                        }
                    }
                }
                using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    if (httpWebResponse.StatusCode == HttpStatusCode.OK)
                    {
                        string text = ReadResponse(httpWebResponse);
                        if (!text.Contains("\"ERROR\""))
                        {
                            return text;
                        }
                        _errors++;
                        if (_debug)
                        {
                            throw new Exception("ATrigger, Error Found In Result: " + text);
                        }
                    }
                    else
                    {
                        var str = $"Status Code {httpWebResponse.StatusCode}. ";
                        str += ReadResponse(httpWebResponse);
                        _errors++;
                        if (_debug)
                        {
                            throw new Exception("ATrigger, Unexpected Status Code: " + str);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _errors++;
                if (_debug)
                {
                    throw new Exception("ATrigger, Unexpected Error: " + ex.Message);
                }
            }
            return string.Empty;
        }

        private string ReadResponse(WebResponse response)
        {
            if (response != null)
            {
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
            return null;
        }
        #endregion
    }
}