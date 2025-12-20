namespace Traincrew_MultiATS_Server.UT.HostedService;


public class InitDbHostedServiceTest
{
    /*
    [Fact]
    public void LockStringParseTest()
    {
        
        var initializer = new DbRendoTableInitializer(
            "TH65", null, null, null, CancellationToken.None);

        var result1 = initializer.CalcLockItems("21");
        List<LockItem> expected1 =
            [new() { IsReverse = NR.Normal, Name = "21", StationId = "TH65" }];
        Assert.Equal(expected1, result1, isEqual);

        var result2 = initializer.CalcLockItems("(21)");
        List<LockItem> expected2 =
            [new() { IsReverse = NR.Reversed, Name = "21", StationId = "TH65" }];
        Assert.Equal(expected2, result2, isEqual);

        var result3 = initializer.CalcLockItems("21(22)");
        List<LockItem> expected3 =
        [
            new() { IsReverse = NR.Normal, Name = "21", StationId = "TH65" },
            new() { IsReverse = NR.Reversed, Name = "22", StationId = "TH65" }
        ];
        Assert.Equal(expected3, result3, isEqual);

        var result4 = initializer.CalcLockItems("50ﾛT ET {48T 但 48}");
        List<LockItem> expected4 =
        [
            new() { IsReverse = NR.Normal, Name = "50ﾛT", StationId = "TH65" },
            new() { IsReverse = NR.Normal, Name = "ET", StationId = "TH65" },
            new()
            {
                Name = "and",
                Children =
                [
                    new() { IsReverse = NR.Normal, Name = "48T", StationId = "TH65" },
                    new() { IsReverse = NR.Normal, Name = "48", StationId = "TH65" }
                ]
            }
        ];
        Assert.Equal(expected4, result4, isEqual);

        var result5 = initializer.CalcLockItems("{[[(12L) 又は (13L)]] 但 99N}");
        List<LockItem> expected5 =
        [
            new()
            {
                Name = "and",
                Children =
                [
                    new()
                    {
                        Name = "or",
                        Children =
                        [
                            new() { IsReverse = NR.Reversed, Name = "12L", StationId = "TH64" },
                            new() { IsReverse = NR.Reversed, Name = "13L", StationId = "TH64" }
                        ]
                    },
                    new() { IsReverse = NR.Normal, Name = "99N", StationId = "TH65" }
                ]
            }
        ];
        Assert.Equal(expected5, result5, isEqual);

        var result6 = initializer.CalcLockItems("{142T 146T 但[(1RC)]}");
        List<LockItem> expected6 =
        [
            new()
            {
                Name = "and",
                Children =
                [
                    new()
                    {
                        Name = "or",
                        Children =
                        [
                            new() { IsReverse = NR.Normal, Name = "142T", StationId = "TH65" },
                            new() { IsReverse = NR.Normal, Name = "146T", StationId = "TH65" },
                        ]
                    },
                    new() { IsReverse = NR.Reversed, Name = "1RC", StationId = "TH66S" },
                ]
            }
        ];
        Assert.Equal(expected6, result6, isEqual);
        var result7 = initializer.CalcLockItems("{[1RT 51ﾛT 52T] 但[51 52 53]}");
        List<LockItem> expected7 =
        [
            new()
            {
                Name = "and",
                Children =
                [
                    new()
                    {
                        Name = "or",
                        Children =
                        [
                            new() { IsReverse = NR.Normal, Name = "1RT", StationId = "TH66S" },
                            new() { IsReverse = NR.Normal, Name = "51ﾛT", StationId = "TH66S" },
                            new() { IsReverse = NR.Normal, Name = "52T", StationId = "TH66S" }
                        ]
                    },
                    new()
                    {
                        Name = "or",
                        Children =
                        [
                            new() { IsReverse = NR.Normal, Name = "51", StationId = "TH66S" },
                            new() { IsReverse = NR.Normal, Name = "52", StationId = "TH66S" },
                            new() { IsReverse = NR.Normal, Name = "53", StationId = "TH66S" }
                        ]
                    }
                ]
            }
        ];
        Assert.Equal(expected7, result7, isEqual);
        
    }

    private bool isEqual(LockItem expected, LockItem actual)
    {
        if (expected.Name is "or" or "and")
        {
            var isSameChildren = expected.Children
                .Select((x, i) => isEqual(x, actual.Children[i]))
                .All(x => x);
            return isSameChildren && expected.Name == actual.Name; 
        }
        return expected.IsReverse == actual.IsReverse &&
               expected.RouteLockGroup == actual.RouteLockGroup &&
               expected.StationId == actual.StationId &&
               expected.Name == actual.Name;
    }
    */
}