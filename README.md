# SqlHelper
Lightweight Stored Procedure wrapper class

## Install

    PM> Install-Package KevsSqlHelper
    
## Usage

    Sql.ConnectionString = "Server=FOO;Database=BAR;User Id=BAZ;Password=BOSH;";

    var spParamList = new List<SqlParameter>
    {
        Sql.Param("username", "herp@derp.com"),
        Sql.Param("password", "password1"),
        Sql.OutParam("userId", sqlDbType: SqlDbType.BigInt)
    };
    
    try
    {
        var result = Sql.CallStoredProcedure("getUserId", spParamList);
        var userId = (long)result["userId"].Value;
        Console.WriteLine(userId);
    }
    catch (Exception e)
    {
        Console.WriteLine($"An exception occurred while executing the getUserId stored procedure: {e}");
        throw;
    }

### A little bit of auto mapping

	var widgets = Sql.CallSpReturningListOf<Widget>("getWidgets", Sql.Param("widgetId", widgetId));

	public class Widget
    {
        public string Doohickey { get; set; }
        public string Thingamajig { get; set; }
    }
