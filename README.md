NoSQL investigation for ***
===================

This project involves investigating two NoSQL databases compared to a traditional SQL database.

* Hadoop, HBase & OpenTSDB
* Cassandra & KairosDB


Directories
------------

h3. MSSQL
SQL-query to be able to export existing data to a proper csv-format for importing data to OpenTSDB & KairosDB.

h3. OpenTSDB
A program that runs queries against OpenTSDB via HTTP-API.

h3. csv_to_json
A program that converts the csv, exported from MSSQL, into JSON format. This is to be able to batch import data to KairosDB.

h3. KairosDB
A program that runs queries against KairosDB via REST-API.

h3. split_csv
A program that splits the csv (every other row), exported from MSSQL, into two new files. This is to make a smoother import to OpenTSDB.