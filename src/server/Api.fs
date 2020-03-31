module Api

open FSharp.Data
open System

[<Literal>]
let FilePath = @"./../../csse_covid_19_data/csse_covid_19_daily_reports/01-22-2020.csv"
type Daily = CsvProvider<FilePath>

let files =
    System.IO.Directory.GetFiles("./../../csse_covid_19_data/csse_covid_19_daily_reports", "*.csv")
    |> Array.map System.IO.Path.GetFullPath

let allData =
    files
    |> Seq.map Daily.Load
    |> Seq.collect(fun data -> data.Rows)
    |> Seq.distinctBy (fun row -> row.``Country/Region``, row.``Province/State``, row.``Last Update``.Date)
    |> Seq.sortBy (fun row -> row.``Last Update``.Date)
    |> Seq.filter (fun row -> row.``Country/Region`` <> "Others")
    |> Seq.toArray

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

let confirmedByCountryDaily =
    [| for country, rows in allData |> Seq.groupBy(fun r -> cleanseCountry r.``Country/Region``) do
        let countryData =
            [| for date, rows in rows |> Seq.groupBy(fun r -> r.``Last Update``.Date) do
                {| Date = date
                   Confirmed = rows |> Seq.sumBy(fun r -> r.Confirmed.GetValueOrDefault 0) |}
            |]
        country, countryData
    |]

let countryLookup = confirmedByCountryDaily |> Map
let allCountries = confirmedByCountryDaily |> Array.map fst