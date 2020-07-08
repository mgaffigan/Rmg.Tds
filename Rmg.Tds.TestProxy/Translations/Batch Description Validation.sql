DECLARE @FacID varchar(6),
	@RxBatch varchar(6),
	@PharmID varchar(6),
	@BatchDesc varchar(250),
	@PhrNPI varchar(255);

-- Match
insert into Rx..RxBatches(FacID, RxBatch, PharmID, BatchDescr, PhrNPI) 
values (@FacID, @RxBatch, @PharmID, @BatchDesc, @PhrNPI)
-- End

-- Replace
IF @BatchDesc = ''
BEGIN
	insert into Rx..RxBatches(FacID, RxBatch, PharmID, BatchDescr, PhrNPI) 
	values (@FacID, @RxBatch, @PharmID, CURRENT_USER, @PhrNPI)
END
ELSE
BEGIN
	insert into Rx..RxBatches(FacID, RxBatch, PharmID, BatchDescr, PhrNPI) 
	values (@FacID, @RxBatch, @PharmID, @BatchDesc, @PhrNPI)
END;
-- End