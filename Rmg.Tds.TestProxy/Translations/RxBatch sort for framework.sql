:setvar OrderBy a.RxBatch
DECLARE @FacID varchar(6) = 'MMCAST'

-- Match
select a.RxBatch, PharmID, BatchDescr, RxCount 
from Rx..RxBatches a 
left outer join (
	select FacID, RxBatch, count(*) as RxCount 
	from Rx..Rxs 
	group by FacID, RxBatch
) b on a.FacID = b.FacID and a.RxBatch = b.RxBatch 
where a.FacID = @FacID 
Order by $(OrderBy)
-- End

-- Replace
select a.RxBatch, PharmID, BatchDescr, RxCount 
from Rx..RxBatches a 
left outer join (
	select FacID, RxBatch, count(*) as RxCount 
	from Rx..Rxs 
	group by FacID, RxBatch
) b on a.FacID = b.FacID and a.RxBatch = b.RxBatch 
where a.FacID = @FacID and a.RxBatch like '[A-Z]%'
Order by 
	CASE
		WHEN a.RxBatch like 'A%' THEN 10
		WHEN a.RxBatch like 'P%' THEN 8
		WHEN a.RxBatch like 'S%' THEN 5
		WHEN a.RxBatch like 'C%' THEN 4
		WHEN a.RxBatch = '_HMDEL' THEN -10
		WHEN a.RxBatch like '[_]HM%' THEN 2
		ELSE 0
	END desc, $(OrderBy)
-- End