open Saturn
open Giraffe

let findByCountry country =
    match Api.countryLookup.TryFind country with
    | Some stats -> stats |> json
    | None -> RequestErrors.NOT_FOUND (sprintf "Unknown country %s" country)

let apiRoutes = router {
    get "/api/countries" (Api.allCountries |> json)
    getf "/api/countries/%s" findByCountry
}


let myApplication = application {
    use_json_serializer (Thoth.Json.Giraffe.ThothSerializer())
    use_router apiRoutes
}

run myApplication

(*

http://localhost:5000/api/countries
http://localhost:5000/api/countries/China
http://localhost:5000/api/countries/UnknownCountry

*)