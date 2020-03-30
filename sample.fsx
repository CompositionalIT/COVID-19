#load @".paket\load\netcoreapp3.1\main.group.fsx"

open XPlot.Plotly
open FSharp.Data
open System

fsi.AddPrinter<DateTime>(fun dt -> dt.ToShortDateString())

[<Literal>]
let V1FilePath = __SOURCE_DIRECTORY__ + @"/csse_covid_19_data/csse_covid_19_daily_reports/01-22-2020.csv"
type DailyV1 = CsvProvider<V1FilePath>
[<Literal>]
let V2FilePath = __SOURCE_DIRECTORY__ + @"/csse_covid_19_data/csse_covid_19_daily_reports/03-22-2020.csv"
type DailyV2 = CsvProvider<V2FilePath>

let files =
    System.IO.Directory.GetFiles("csse_covid_19_data/csse_covid_19_daily_reports", "*.csv")
    |> Seq.filter(fun r ->
        let r = System.IO.Path.GetFileNameWithoutExtension r
        System.DateTime.ParseExact(r, "MM-dd-yyyy", null) <= (DateTime(2020,3,21)))
    |> Seq.map System.IO.Path.GetFullPath


let makeScatter (country, values) =
    let dates, values = values |> Seq.toArray |> Array.unzip
    let t = Scatter(x = dates, y = values) :> Trace
    t.name <- country
    t

let allData =
    files
    |> Seq.map DailyV1.Load
    |> Seq.collect(fun data -> data.Rows)
    |> Seq.distinctBy (fun row -> row.``Country/Region``, row.``Province/State``, row.``Last Update``.Date)
    |> Seq.sortBy (fun row -> row.``Last Update``.Date)
    |> Seq.filter (fun row -> row.``Country/Region`` <> "Others")

let cleanseCountry (country:string) =
    match country.Trim() with
    | "Russia" -> "Russian Federation"
    | "Iran" -> "Iran (Islamic Republic of)"
    | "Macau" -> "Macao SAR"
    | "Hong Kong" -> "Hong Kong SAR"
    | "Viet Nam" -> "Vietnam"
    | "Palestine" -> "occupied Palestinian territory"
    | "Korea, South" -> "South Korea"
    | "Republic of Korea" -> "South Korea"
    | "Unite States" -> "US"
    | "Mainland China" -> "China"
    | "UK" -> "United Kingdom"
    | country -> country

let confirmedByCountryDaily = seq {
    for country, rows in allData |> Seq.groupBy(fun r -> cleanseCountry r.``Country/Region``) do
        let countryData = seq {
            for date, rows in rows |> Seq.groupBy(fun r -> r.``Last Update``.Date) do
                date, rows |> Seq.sumBy(fun r -> r.Confirmed.GetValueOrDefault 0)
        }
        country, countryData
}

let topTen =
    confirmedByCountryDaily
    |> Seq.sortByDescending(fun (_, rows) -> rows |> Seq.map snd |> Seq.max)
    |> Seq.skip 1
    |> Seq.take 10

topTen
|> Seq.map makeScatter
|> Chart.Plot
|> Chart.Show
