open Saturn
open Giraffe

module ApiRoutes =
    let findByCountry country =
        match Api.countryLookup.TryFind country with
        | Some stats -> stats |> json
        | None -> RequestErrors.NOT_FOUND (sprintf "Unknown country %s" country)

    let apiRouter = router {
        get "/countries" (Api.allCountries |> json)
        getf "/countries/%s" findByCountry
    }

module UiRoutes =
    open GiraffeViewEngine
    open Zanaptak.TypedCssClasses

    type Bulma = CssClasses<"https://cdn.jsdelivr.net/npm/bulma@0.8.2/css/bulma.min.css">
    let (++) a b = a + " " + b
    let _classes attributes = attributes |> String.concat " " |> _class
    let createPage title subtitle contents =
        html [] [
            head [] [
                link [ _rel "stylesheet"; _href "https://cdn.jsdelivr.net/npm/bulma@0.8.2/css/bulma.min.css" ]
            ]
            body [] [
                section [ _classes [ Bulma.hero; Bulma.``is-primary`` ] ] [
                    div [ _class Bulma.``hero-body`` ] [
                        div [ _class Bulma.container ] [
                            h1 [ _class Bulma.title ] [ Text title ]
                            h2 [ _class Bulma.subtitle ] [ Text subtitle ]
                        ]
                    ]
                ]
                section [ _class Bulma.section ] [
                    div [ _class Bulma.container ]
                        contents
                ]
            ]
        ]
    let countriesView =
        createPage "COVID 19 Dataset" "Written in F#" [
            table [ _class Bulma.table ] [
                thead [] [
                    tr [] [
                        th [] [ Text "Country" ]
                        th [] [ Text "Deaths" ]
                        th [] [ Text "Recovered" ]
                        th [] [ Text "Confirmed" ]
                    ]
                ]
                tbody [] [
                    for row in Api.countryStats |> Array.sortByDescending(fun row -> row.Deaths) do
                        tr [] [
                            td [] [ a [ _href (sprintf "/%s" row.Country) ] [ Text row.Country ] ]
                            td [] [ Text (row.Deaths.ToString "N0") ]
                            td [] [ Text (row.Recovered.ToString "N0") ]
                            td [] [ Text (row.Confirmed.ToString "N0") ]
                        ]
                ]
            ]
            for country in Api.allCountries do
                div [] [ Text country ]
        ]
    let countryView country =
        createPage country "Deaths over time" [
            match Api.countryLookup.TryFind country with
            | Some stats ->
                let chart =
                    stats
                    |> Array.map(fun s -> s.Date, s.Deaths)
                    |> XPlot.Plotly.Chart.Line
                chart.GetHtml() |> Text
            | None ->
                Text "No data for country!"
        ]

    let uiRouter = router {
        get "/" (htmlView countriesView)
        getf "/%s" (countryView >> htmlView)
    }

let appRouter = router {
    forward "/api" ApiRoutes.apiRouter
    forward "" UiRoutes.uiRouter
}

let myApplication = application {
    use_json_serializer (Thoth.Json.Giraffe.ThothSerializer())
    use_router appRouter
}

run myApplication

(*

http://localhost:5000/api/countries
http://localhost:5000/api/countries/China
http://localhost:5000/api/countries/UnknownCountry

*)