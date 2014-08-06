using System;
using System.IO;
using System.Net;
using System.Text;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using System.Collections.Generic;

using System.Globalization;
//using System.Threading;

/*
 * Testing a simple query for metrics with certain tags between a specific timespan
 * and prints out average, sum and max value.
 * */

namespace KairosDB
{
    public class WebRequestPostExample
    {
        // Global constans
        const string request_url = "http://localhost:8080/api/v1/datapoints/query/";
        const string metrics_url = "http://localhost:8080/api/v1/metricnames";
        const string tagks_url = "http://localhost:8080/api/v1/tagnames";
        const string tagv_url = "http://localhost:8080/api/v1/tagvalues";

        // Create JSON-objects for troubleshooting
        static string valid_metrics = "[" + new WebClient().DownloadString(metrics_url) + "]";
        static string valid_tagks = "[" + new WebClient().DownloadString(tagks_url) + "]";
        static string valid_tagvs = "[" + new WebClient().DownloadString(tagv_url) + "]";
        static MetricList[] json_metrics = JsonConvert.DeserializeObject<MetricList[]>(valid_metrics);
        static TagkList[] json_tagks = JsonConvert.DeserializeObject<TagkList[]>(valid_tagks);
        static TagvList[] json_tagvs = JsonConvert.DeserializeObject<TagvList[]>(valid_tagvs);

        public static void Main()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("******************************\n*      Testing KairosDB      *\n******************************");
            Console.ResetColor();
            Console.Write("Type \"");
            highlight("help");
            Console.WriteLine("\" at any point.\n");

            while (true)
            {
                // Read inputs
                string[] metrics = readMetrics();
                string[] tags = readTags();
                string timespan = readTimespan();

                // Create JSON-string of input and send to server
                string queryMetrics = createJSON(metrics, tags);
                Response response_json = queryToResponse(queryMetrics, timespan);

                // Print results
                printResults(response_json);

                highlight("\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");
            }
        }

        static string ReadLine()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            string input = Console.ReadLine();
            Console.ResetColor();
            return input;
        }

        static string[] readMetrics()
        {
            Console.WriteLine("Type in at least 1 metric, separated by space:");
            while (true)
            {
                string input = ReadLine(); // Console.ReadLine();
                string[] metricArr = input.Split(' ');

                if (input == "help")
                    printMetrics();

                else
                {
                    if (metricArrIsValid(metricArr))
                        return metricArr;
                }
            }
        }

        static string[] readTags()
        {
            Console.WriteLine("\nType in key/value-tags (<tagk>=<tagv>) separated by space:");
            while (true)
            {
                string input = ReadLine(); // Console.ReadLine();
                string[] tagArr = input.Split(' ');

                // Allow no tags
                if (input == "")
                    return tagArr;

                else if (input == "help")
                {
                    Console.Write("Type \"");
                    highlight("keys");
                    Console.Write("\" for a list of valid tag-keys, or type \"");
                    highlight("values");
                    Console.WriteLine("\" for a list of valid tag-values:");

                    input = ReadLine(); // Console.ReadLine();
                    if (input == "keys")
                        printTagks();
                    else if (input == "values")
                        printTagvs();
                }

                else
                {
                    if (tagArrIsValid(tagArr))
                        return tagArr;
                }
            }
        }

        static string readTimespan()
        {
            Console.WriteLine("\nType in timespan <starttime> <endtime> separated with space:");
            string input = ReadLine();
            string[] timespan = input.Split(' ');

            DateTime starttime = new DateTime();
            DateTime endtime = new DateTime();

            // Is input valid?
            while (!DateTime.TryParse(timespan[0], out starttime) || starttime < new DateTime(1970,01,02) || starttime > DateTime.Now ||
                (timespan.Length > 1 && (!DateTime.TryParse(timespan[1], out endtime) || starttime > endtime)))
            {
                if (input == "")
                    highlight("You must at least specify starttime:\n");
                else if (starttime < new DateTime(1970, 01, 02))
                    highlight("Starttime must be > 1970-01-01\n");
                else if (starttime > endtime)
                    highlight("Starttime must be smaller than endtime, try again:\n");
                else
                    highlight("You must type in a valid timespan in a valid time-format:\n");

                input = ReadLine();
                timespan = input.Split(' ');
            }

            // Convert to unixtime
            Int64 unixTime_start = (Int64)DateTimeToUnixTimestamp(starttime) * 1000;
            Int64 unixTime_end;

            // Use current unix-timestamp if user doesn't specify an endtime
            if (timespan.Length < 2)
            {
                unixTime_end = (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                return "\"cache_time\":0,\"start_absolute\":" + unixTime_start + ",\"end_absolute\":" + unixTime_end;
            }

            // Convert to unixtime
            unixTime_end = (Int64)DateTimeToUnixTimestamp(endtime) * 1000;
            return "\"cache_time\":0,\"start_absolute\":" + unixTime_start + ",\"end_absolute\":" + unixTime_end;
        }

        static string createJSON(string[] metrics, string[] tags)
        {
            string queryMetrics = "";
            foreach (string metric in metrics)
            {
                Metric metric_obj = new Metric();
                metric_obj.tags = new Dictionary<string, List<string>>();
                foreach (string tag in tags)
                {
                    if (!tag.Contains("="))
                        break;
                    int pos_deli = tag.IndexOf('=');
                    string key = tag.Substring(0, pos_deli);
                    string value = tag.Substring(pos_deli + 1);
                    if (metric_obj.tags.ContainsKey(key))
                        metric_obj.tags[key].Add(value);
                    else
                        metric_obj.tags.Add(key, new List<string>() { value });
                }

                metric_obj.name = metric;
                queryMetrics += JsonConvert.SerializeObject(metric_obj) + ',';
            }
            return queryMetrics.Remove(queryMetrics.Length - 1);
        }

        static Response queryToResponse(string queryMetrics, string timespan)
        {
            WebRequest request = WebRequest.Create(request_url);
            request.Method = "POST";
            string postQuery = "{\"metrics\":[" + queryMetrics + "]," + timespan + "}";
            byte[] byteArray = Encoding.UTF8.GetBytes(postQuery);
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            WebResponse response = request.GetResponse();
            Console.WriteLine("Http Webresponse: [" + ((HttpWebResponse)response).StatusDescription + ']');
            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();

            reader.Close();
            dataStream.Close();
            response.Close();

            return JsonConvert.DeserializeObject<Response>(responseFromServer);
        }

        static void highlight(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(message);
            Console.ResetColor();
        }

        static void printMetrics()
        {
            Console.WriteLine("\nValid metrics:");
            foreach (string metric in json_metrics[0].results)
            {
                // Don't care about KairosDB's own metrics
                if (metric.Length < 9 || metric.Substring(0,9) != "kairosdb.")
                    Console.WriteLine(metric);
            }
            Console.WriteLine("");
        }

        static void printTagks()
        {
            Console.WriteLine("\nValid tag-keys:");
            foreach (string tagkey in json_tagks[0].results)
            {
                // Don't care about KairosDB's own tags
                if (!"buffer host method query_index request".Contains(tagkey) && tagkey != "metric_name")
                    Console.WriteLine(tagkey);
            }
            Console.WriteLine("");
        }

        static void printTagvs()
        {
            Console.WriteLine("\nValid tag-values:");
            foreach (string tagvalue in json_tagvs[0].results)
            {
                // Don't care about KairosDB's own tags
                if ((tagvalue.Length < 9 || tagvalue.Substring(0, 9) != "kairosdb.") && (tagvalue.Length < 5 || tagvalue.Substring(0, 5) != "query"))
                    Console.WriteLine(tagvalue);
            }
            Console.WriteLine("");
        }

        static void printResults(Response response_json)
        {
            Console.WriteLine("\nCollected datapoints: " + response_json.queries[0].sample_size);
            if (response_json.queries[0].sample_size != 0)
            {
                foreach (Query query in response_json.queries)
                {
                    foreach (Result result in query.results)
                    {
                        long info_out = 0;
                        int counts = 0;
                        int max = Int32.MinValue;
                        foreach (long[] value in result.values)
                        {
                            ++counts;
                            info_out += value[1];
                            if (value[1] > max)
                                max = (int)value[1];
                        }

                        Console.WriteLine("\nMetric: " + result.name);
                        Console.WriteLine("Sum of values: " + info_out);
                        info_out /= counts;
                        Console.WriteLine("Average value: " + info_out);
                        Console.WriteLine("Max value: " + max);
                    }
                }
            }
        }

        static bool metricArrIsValid(string[] metricArr)
        {
            foreach (string metric in metricArr)
            {
                if (!Array.Exists(json_metrics[0].results, element => element == metric))
                {
                    highlight("Invalid metric, type \"help\" for a list of valid metrics.");
                    Console.WriteLine("");
                    return false;
                }
            }

            return true;
        }

        static bool tagArrIsValid(string[] tagArr)
        {
            foreach (string tag in tagArr)
            {
                if (!tag.Contains("="))
                {
                    highlight("Invalid tag, type \"help\" for a list of valid tags.");
                    Console.WriteLine("");
                    return false;
                }

                int pos_deli = tag.IndexOf('=');
                string key = tag.Substring(0,pos_deli);
                string value = tag.Substring(pos_deli+1);

                if ((!Array.Exists(json_tagks[0].results, element => element == key)) ||
                    (!Array.Exists(json_tagvs[0].results, element => element == value)))
                {
                    highlight("Invalid tag, type \"help\" for a list of valid tags.");
                    Console.WriteLine("");
                    return false;
                }
            }

            return true;
        }

        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }
    }

    public class MetricList
    {
        public string[] results { get; set; }
    }

    public class TagkList
    {
        public string[] results { get; set; }
    }

    public class TagvList
    {
        public string[] results { get; set; }
    }

    public class Metric
    {
        public System.Collections.Generic.Dictionary<string, List<string>> tags { get; set; }
        public string name { get; set; }
    }


    public class Response
    {
        public Query[] queries { get; set; }
    }

    public class Query
    {
        public int sample_size { get; set; }
        public Result[] results { get; set; }
    }

    public class Result
    {
        public string name { get; set; }
        public System.Collections.Generic.Dictionary<string, List<string>> tags { get; set; }
        public long[][] values { get; set; }
    }

}