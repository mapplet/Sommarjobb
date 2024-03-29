SELECT O.ObservablePropertyPID AS 'Metric'
	  ,DATEDIFF(SECOND,{d '1970-01-01'}, DATEADD(MILLISECOND, -DATEPART(MILLISECOND, O.ObservationTimestamp), O.ObservationTimestamp)) AS 'Unixtime'
	  ,AVG(O.Value) AS 'Value'
	  ,CASE WHEN O.ContextPID IS NULL OR O.ContextPID = ''
		THEN 'name=Unknown'
		ELSE REPLACE(
				REPLACE(CASE WHEN O.ContextPID LIKE 'HTTP://%'
				THEN 'name=' + SUBSTRING(O.ContextPID,8,LEN(O.ContextPID)-8)
				ELSE 'name=' + O.ContextPID END, ' ', '_')
			, ':', '/')
		END AS 'NameTag'
		,'type=' + REPLACE(OP.ObservablePropertyType, ' ', '_') AS 'TypeTag'
  FROM [ThePortalObservationDb_2013_2].[dbo].[Observation] O, [ThePortalObservationDb_2013_2].[dbo].[ObservableProperty] OP
  WHERE O.ObservablePropertyPID != 'timestamp'
		AND O.ObservablePropertyID = OP.ID
  GROUP BY O.ObservablePropertyPID, DATEADD(MILLISECOND, -DATEPART(MILLISECOND, O.ObservationTimestamp), O.ObservationTimestamp), O.ContextPID, [ObservablePropertyType]
  ORDER BY 'Metric', 'Unixtime'