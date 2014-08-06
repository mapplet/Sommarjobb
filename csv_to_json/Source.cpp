#include <iostream>
#include <fstream>
#include <string>
#include <vector>

using namespace std;

/*
Metric object

"<metric> <unixtime> <value> <tagk1>=<tagv1> <tagk2>=<tagv2>"

to convert into JSON:
"
{
	"name":"<metric>",
	"tags":{"<tagk1>":"<tagv1>","<tagk2>":"<tagv2>"},
	"datapoints":	[[<unixtime>,<value1>],[<unixtime>,<value2>],[<unixtime>,<valueN>]]
}
"
*/

class Metric {

public:
	Metric(string name, string tags)
	{ _name = name; _tags = tags; }

	void append_datapoint(string &unixtime, string &datapoint);

	string get_name() { return _name; };
	string get_tags() { return _tags; };
	string get_datapoints() { return _datapoints; };

private:
	string _name;
	string _tags;
	string _datapoints;
};

void Metric::append_datapoint (string &unixtime, string &datapoint)
{
	if (_datapoints != "")
		_datapoints.append(",");
	_datapoints.append("[" + unixtime + "000," + datapoint + "]");
}


ostream& operator<<(std::ostream& os, Metric& m)
{
    // Write object to stream
	os << "{\"name\":\"" << m.get_name() << "\",\"tags\":{" << m.get_tags() << "},\"datapoints\":[" << m.get_datapoints() << "]}";

  return os;
}


int main()
{

  string path_to_file;

  std::cout << "Type in full path to CSV-file to convert into JSON:\n(\"default\" for data.txt at desktop)" << endl;
  cin >> path_to_file;

  // Hårdkodat..
  if (path_to_file == "default")
	path_to_file = "C:\\Users\\martin.norberg\\Desktop\\data.txt";

  std::cout << "Trying to open \"" << path_to_file << "\".     ";

  ifstream csv(path_to_file);
  
  if (!csv)
  {
	  std::cout << "[FAILED]" << endl;
	  return -1;
  }

  std::cout << "[OK]" << endl;

  string metric;
  string unixtime;
  string value;
  string name_tag;
  string type_tag;
  vector<Metric> metric_vector;

  std::cout << "# Wait while creating objects of metrics.." << endl;

  int processed_lines = 0;

  // Go trough all rows in CSV-file..
  while(csv >> metric >> unixtime >> value >> name_tag >> type_tag)
  {
	  // Fix format for tags
	  string tags = "\"name\":\"" + name_tag.erase(0,5) + "\",\"type\":\"" + type_tag.erase(0,5) + "\"";
	  bool found_metric = false;

	  // See if there is a matching metric to add value to
	  for (unsigned int i=0; i<metric_vector.size(); ++i)
	  {
		  Metric &curr_metric = metric_vector.at(i);

		  // If there is a match, add value to this object..
		  if (curr_metric.get_name() == metric && curr_metric.get_tags() == tags)
		  {
			  curr_metric.append_datapoint(unixtime, value);
			  found_metric = true;
			  break;
		  }
	  }

	  // If not, create a new one..
	  if (!found_metric)
	  {
		  Metric new_metric(metric, tags);
		  new_metric.append_datapoint(unixtime, value);
		  metric_vector.push_back(new_metric);
	  }
	  
	  ++processed_lines;

	  if ((processed_lines % 100000) == 0)
		  std::cout << "  # " << processed_lines << " lines processed" << endl;
  }

  csv.close();

  std::cout << "[OK]" << endl;
  std::cout << "# Creating JSON-file..     ";

  // Write result to this file
  ofstream json_file ("C:\\Users\\martin.norberg\\Desktop\\json-export.txt");

  for (unsigned int i=0; i<metric_vector.size(); ++i)
  {
	  json_file << metric_vector.at(i) << "\n";
  }

  json_file.close();
  std::cout << "[OK]" << endl;

  std::cout << "# Job done!" << endl;

  return 0;
}