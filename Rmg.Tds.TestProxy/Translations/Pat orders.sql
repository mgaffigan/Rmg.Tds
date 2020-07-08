DECLARE @facId varchar(6),
  @patId varchar(11),
  @filterOnCutoffDate bit,
  @cutoffDate datetime,
  @isRetail bit,
  @nsId varchar(6),
  @useNsHouseStock bit,
  @facHouseStockPrivateOnly bit,
  @nsHouseStockPrivateOnly bit;
-- Match
exec PAT.GetOrdersForBrowse @facId, @patId, @pharmacyIds, @filterOnCutoffDate, @cutoffDate,  @isRetail, @nsId, @useNsHouseStock, @facHouseStockPrivateOnly, @nsHouseStockPrivateOnly
-- End
-- Replace
exec ITP..GetOrdersForBrowse @facId, @patId, @filterOnCutoffDate, @cutoffDate,  @isRetail, @nsId, @useNsHouseStock, @facHouseStockPrivateOnly, @nsHouseStockPrivateOnly
-- End
