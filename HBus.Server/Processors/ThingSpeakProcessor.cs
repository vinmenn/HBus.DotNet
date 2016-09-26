using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Web;
using HBus.Server.Data;

namespace HBus.Server.Processors
{
    /// <summary>
    /// send data to ThingSpeak service
    /// </summary>
    /// <remarks>based on code found on </remarks>
    public class ThingSpeakProcessor: BaseProcessor
    {
        private const string Url = "http://api.thingspeak.com/";

        private readonly string _apiKey;
        private readonly string[] _fields;
        private float[] _values;
        private int _total;

        public ThingSpeakProcessor(string apiKey, string[] fields)
        {
            _apiKey = apiKey;
            _fields = fields;
            if (_fields != null)
                _values = new float[_fields.Length];
            _total = 0;

            //Event from ep source
            OnSourceEvent = WriteEvent;

            Log.Debug("ThingSpeak endpoint created");
        }

        public void WriteEvent(Event @event, BaseProcessor sender)
        {
            if (_fields != null)
            {
                for (int i = 0; i < _fields.Length; i++)
                {
                    if (_fields[i] == @event.Source)
                    {
                        _values[i] = @event.Value;
                        _total++;
                        break;
                    }
                }
                if (_total == (_fields.Length - 1))
                {
                    PostToThingSpeak(_values);
                    _total = 0;
                }
            }
        }
        private void PostToThingSpeak(float[] values)
        {
            var sbResponse = new StringBuilder();
            var buf = new byte[8192];

            try
            {
                var postMsg = Url + "update?key=" + _apiKey;

                for (var i = 0; i < values.Length; i++)
                {
                    postMsg += String.Format("&field{0}={1}", i + 1, HttpUtility.UrlEncode(values[i].ToString(CultureInfo.InvariantCulture)));
                }

                // Hit the URL with the querystring and put the response in webResponse
                var myRequest = (HttpWebRequest)WebRequest.Create(postMsg);
                var webResponse = (HttpWebResponse)myRequest.GetResponse();

                var myResponse = webResponse.GetResponseStream();
                var count = 0;
 
                // Read the response buffer and return
                do
                {
                    if (myResponse != null) 
                        count = myResponse.Read(buf, 0, buf.Length);

                    if (count != 0)
                    {
                        sbResponse.Append(Encoding.ASCII.GetString(buf, 0, count));
                    }
                } while (count > 0);

                Log.Debug("Thingspeak response: " + sbResponse);
            }
            catch (WebException ex)
            {
                Log.Error("Sending data to thingspeak failed", ex);
            }
 
        }
    }
}