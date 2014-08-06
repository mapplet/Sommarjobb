#include <iostream>
#include <fstream>
#include <string>
#include <vector>

using namespace std;

void incr_lineCount (int &lines_processed)
{
	++lines_processed;
	if ((lines_processed % 100000) == 0)
		cout << "# " << lines_processed << " rows processed" << endl;
}

int main()
{

  string path_to_file;

  std::cout << "Type in full path to CSV-file to split:\n(\"SEAM4US\" for SEAM4USdata.txt at desktop)" << endl;
  cin >> path_to_file;

  // Hårdkodat..
  if (path_to_file == "SEAM4US")
	path_to_file = "C:\\Users\\martin.norberg\\Desktop\\SEAM4USdata.txt";

  std::cout << "Trying to open \"" << path_to_file << "\".     ";

  ifstream csv(path_to_file);
  
  if (!csv)
  {
	  std::cout << "[FAILED]" << endl;
	  return -1;
  }

  std::cout << "[OK]" << endl;

  string row = "";
  ofstream output1 ("C:\\Users\\martin.norberg\\Desktop\\output1.txt");
  ofstream output2 ("C:\\Users\\martin.norberg\\Desktop\\output2.txt");

  int lines_processed = 0;

  // Go trough all rows in CSV-file..
  while(getline(csv, row))
  {
	  output1 << row << "\n";
	  incr_lineCount(lines_processed);
	  if (getline(csv, row))
	  {
		  output2 << row << "\n";
		  incr_lineCount(lines_processed);
	  }
  }

  csv.close();
  output1.close();
  output2.close();

  std::cout << "# Job done! " << lines_processed << " rows were processed." << endl;


  return 0;
}