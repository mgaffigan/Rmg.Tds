DECLARE @RxNo int = 11874696
-- Match
SELECT * FROM FWDB.RX.Rxs WHERE RxNo = @RxNo
-- End
-- Replace
SELECT * FROM FWDB.RX.Rxs WHERE RxNo = @RxNo and RoNo <> @RxNo
-- End