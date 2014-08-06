using System;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Threading;


namespace OpenTSDB
{
    class Program
    {
        const string metrics_url = "http://localhost:4240/api/suggest?type=metrics&max=100";
        const string tagks_url = "http://localhost:4240/api/suggest?type=tagk";
        const string tagvs_url = "http://localhost:4240/api/suggest?type=tagv&max=1000";

        static string valid_metrics = new WebClient().DownloadString(metrics_url);
        static string valid_tagks = new WebClient().DownloadString(tagks_url);
        static string valid_tagvs = new WebClient().DownloadString(tagvs_url);

        static void Main()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("******************************\n*      Testing OpenTSDB      *\n******************************\n");
            Console.ResetColor();

            while (true)
            {
                string metric = readMetric();
                string tags = readTags();
                string timespan = readTimespan();

                // Send query to server and recieve response
                string response = queryToResponse(metric, tags, timespan);

                printResults(response, metric, tags);
            }
        }

        static string ReadLine()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            string input = Console.ReadLine();
            Console.ResetColor();
            return input;
        }

        static void highlight(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(message);
            Console.ResetColor();
        }

        static string readMetric()
        {
            Console.WriteLine("Type in metric:");
            string metric = ReadLine();
            while (!valid_metrics.Contains('\"' + metric + '\"'))
            {
                Console.WriteLine("Not a valid metric, try again:");
                metric = ReadLine();
            }

            return metric;
        }

        static string readTags()
        {
            Console.WriteLine("Type key/value-tags separated by space (\"name=S55 type=PM10\" for instance..):");
            string tagsIn = ReadLine();
            string tags = "";

            if (tagsIn != "")
            {
                string[] tagArr = tagsIn.Split(' ');

                for (int i = 0; i < tagArr.Length; ++i)
                {
                    while (!valid_tagks.Contains('\"' + tagArr[i].Split('=')[0] + '\"') || !valid_tagvs.Contains('\"' + tagArr[i].Split('=')[1] + '\"'))
                    {
                        if (!valid_tagks.Contains('\"' + tagArr[i].Split('=')[0] + '\"'))
                            Console.WriteLine("{0} is not a valid tag-key.", tagArr[i].Split('=')[0]);
                        if (tagArr.Length > 1 && !valid_tagvs.Contains('\"' + tagArr[i].Split('=')[1] + '\"'))
                            Console.WriteLine("{0} is not a valid tag-value.", tagArr[i].Split('=')[1]);
                        Console.WriteLine("Try again:");
                        tagsIn = ReadLine();
                        tagArr = tagsIn.Split(' ');
                    }
                }

                for (int i = 0; i < tagArr.Length - 1; ++i)
                    tags += (tagArr[i] + ',');
                tags += tagArr[tagArr.Length - 1];
            }

            return tags;
        }
        
        static string readTimespan()
        {
            /* INGEN FELKONTROLL HÄR */
            /*
            Console.WriteLine("Type in start-time and end-time (2014/04/10-00:00:00, 1401285230, 1h-ago...) separated with space. Leave end-time blank to use a timespan until now:");
            string timeIn = ReadLine();
            string[] timeArr = timeIn.Split(' ');
            string timespan = "";
            for (int i = 0; i < timeArr.Length - 1; ++i)
                timespan += (timeArr[i] + "&end=");
            //timespan += timeArr[timeArr.Length - 1];
            return (timespan += timeArr[timeArr.Length - 1]);
            */
            Console.WriteLine("\nType in timespan <starttime> <endtime> separated with space:");
            string input = ReadLine();
            string[] timespan = input.Split(' ');

            DateTime starttime = new DateTime();
            DateTime endtime = new DateTime();

            // Is input valid?
            while (!DateTime.TryParse(timespan[0], out starttime) || starttime < new DateTime(1970, 01, 02) || starttime > DateTime.Now ||
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
            Int64 unixTime_start = (Int64)DateTimeToUnixTimestamp(starttime);
            Int64 unixTime_end;

            // Use current unix-timestamp if user doesn't specify an endtime
            if (timespan.Length < 2)
                unixTime_end = (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            // Else convert endtime to unixtime
            else
                unixTime_end = (Int64)DateTimeToUnixTimestamp(endtime);
            
            return "start=" + unixTime_start + "&end=" + unixTime_end;
        }

        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }

        static string queryToResponse(string metric, string tags, string timespan)
        {
            //Get datapoints as JSON..
            string request = "http://localhost:4240/api/query?" + timespan + "&m=avg:" + metric + '{' + tags + '}';
            string response = new WebClient().DownloadString(request);

            return response;
        }

        static void printResults(string response, string metric, string tags)
        {
            if (response.Equals("[]"))
                Console.WriteLine("\nThe query did not return any datapoints.");
            else
            {
                //Deserialize the response string into JSON
                var json = JsonConvert.DeserializeObject<Metric[]>(response);

                //stopWatch.Start();
                result_storage avg = getAverage(json);
                //result_storage sum = getSum(json);
                //result_storage max = getMax(json);
                //stopWatch.Stop();

                Console.WriteLine("\nAverage {0} ({2}): {1}", metric, avg.value, tags);
                //Console.WriteLine("Sum of averages: " + sum.value);
                //Console.WriteLine("Max of averages: " + max.value);
                Console.WriteLine("Datapoints: " + avg.datapoints);

                /*// Get the elapsed time as a TimeSpan value.
                TimeSpan ts = stopWatch.Elapsed;

                // Format and display the TimeSpan value. 
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                Console.WriteLine("RunTime " + elapsedTime);*/
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");
            Console.ResetColor();
        }

        static result_storage getAverage(Metric[] json)
        {
            float result = 0;
            int datapoints = 0;

            foreach (float value in json[0].dps.Values)
            {
                ++datapoints;
                result += value;
            }

            result = result / datapoints;

            result_storage toReturn = new result_storage();
            toReturn.value = result;
            toReturn.datapoints = datapoints;

            return toReturn;
        }
        
        static result_storage getSum(Metric[] json)
        {
            float result = 0;
            int datapoints = 0;

            foreach (float value in json[0].dps.Values)
            {
                ++datapoints;
                result += value;
            }

            result_storage toReturn = new result_storage();
            toReturn.value = result;
            toReturn.datapoints = datapoints;

            return toReturn;
        }

        static result_storage getMax(Metric[] json)
        {
            float result = 0;
            int datapoints = 0;

            foreach (float value in json[0].dps.Values)
            {
                ++datapoints;
                if (result < value)
                    result = value;
            }

            result_storage toReturn = new result_storage();
            toReturn.value = result;
            toReturn.datapoints = datapoints;

            return toReturn;
        }

    }

    public class Metric
    {
        public string metric { get; set; }
        public System.Collections.Generic.Dictionary<string, string> tags { get; set; }
        public string[] aggregateTags { get; set; }
        public System.Collections.Generic.Dictionary<int, float> dps { get; set; }
    }

    public struct result_storage
    {
        public int datapoints;
        public float value;
    };

}
