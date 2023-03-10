using MySqlConnector;

namespace tests.integration;

[Collection("Database collection")]
public class SimpleTest
{
    private readonly DatabaseFixture fixture;

    public SimpleTest(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public void SimpleDo()
    {
        using (var cmd = fixture.Db.CreateCommand())
        {
            cmd.CommandText = "DO 1";
            Assert.Equal(0, cmd.ExecuteNonQuery());
        }
    }

    [Fact]
    public void SimpleSelect()
    {
        using (var cmd = fixture.Db.CreateCommand())
        {
            cmd.CommandText = "select 1";

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) reader.GetInt32(0);

            reader.Close();
        }
    }

    [Fact]
    public async Task<int> SimpleSelectMultiFields()
    {
        var total = 0;
        using (var cmd = fixture.Db.CreateCommand())
        {
            cmd.CommandText = "select * FROM test100";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                for (var i = 0; i < 100; i++)
                    total += reader.GetInt32(i);

            reader.Close();
        }

        return total;
    }

    [Fact]
    public int SimpleSelect1000Rows()
    {
        var total = 0;
        int loop;
        for (loop = 0; loop < 100; loop++)
            using (var cmd = fixture.Db.CreateCommand())
            {
                cmd.CommandText = "select * from 1000rows";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        total += reader.GetInt32(0);
                        reader.GetString(1);
                    }
                }
            }

        return total;
    }

    [Fact]
    public int SimpleSelect1000RowsMysql()
    {
        var total = 0;
        int loop;
        using (var con = new MySqlConnection(fixture.DefaultConnString))
        {
            con.Open();
            for (loop = 0; loop < 1000; loop++)
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "select * from 1000rows";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            total += reader.GetInt32(0);
                            reader.GetString(1);
                        }
                    }
                }

            return total;
        }
    }
}