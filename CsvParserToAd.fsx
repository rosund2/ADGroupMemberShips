#r "nuget: FSharp.Data"
#r "nuget: FsPrettyTable"

#load "CsvFilterConfig.fsx"

open CsvFilterConfig
open FSharp.Data

fsi.ShowDeclarationValues <- false

let csv = CsvFile.Load(hasHeaders=false, separators = ";" , uri = __SOURCE_DIRECTORY__ + "\Atest.csv").Cache()

let csvrows = 
    [for row in csv.Rows do 
        let m = Map.empty
        m 
        |> Map.add "group" (row.GetColumn(0)) 
        |> Map.add "upn" (row.GetColumn(1)) 
        |> Map.add "company" (row.GetColumn(2)) 
        |> Map.add "title" (row.GetColumn(3)) 
        |> Map.add "type" (row.GetColumn(4))             
    ]


let unique l k = 
    l
    |> List.map (fun (m:Map<string,string>) -> 
        m.[k]
    ) 
    |> Set.ofList


let mapHasValue k l (m:Map<string,string>) = 
    l 
    |> List.filter (fun v -> 
        if m.ContainsKey k then
            m.[k] = v
        else
            false
    )
    |> (fun l ->        
        not (List.isEmpty l) )


let filterMapListByKey k lf l = List.filter (mapHasValue k lf) l


let companyFilter lcompanies csvrows  = filterMapListByKey "company" lcompanies csvrows// List.filter (mapfilter "company" cl) rows
let employeeFilter ltypes = filterMapListByKey "type" ltypes

type CsvFilterResult = {Rows: list<Map<string,string>> ; EmployeeCount: int; Filter : CsvFilter}

let employeeCount cvsRows = 
    Seq.length (unique cvsRows "upn")

let filterToResult lstcsv  (filter:CsvFilter) = 
    let res = 
        lstcsv
        |> companyFilter filter.Companies
        |> employeeFilter filter.EmployeeTypes
    {Rows = res; EmployeeCount = (employeeCount res); Filter = filter}


let groupByAdGroupName (res:CsvFilterResult) = 
    res.Rows
    |> List.groupBy (fun (m:Map<string,string>) -> 
        m.["group"]
    )
  
type AdGroup = {Rows:list<Map<string,string>>; Count: int; GroupName:string;}
type AdGroupStatistics = {AdGroup:AdGroup; EmployeePercentage: double;}
type CsvFilterResultWithAdGroupStatistics = {Rows: list<Map<string,string>> ; EmployeeCount: int;AdGroupsWithStatics : list<AdGroupStatistics>; Filter:CsvFilter}

let processFilterToResult csvrows filter = 
    let fr = filterToResult csvrows filter

    fr
    |> groupByAdGroupName
        
    |> List.map (fun (adgroup:string, members:list<Map<string,string>>) -> 
        {Rows = members; Count = List.length members; GroupName = adgroup;}
    )
    // makeStatisticsOnGroupResult
    |> List.map (fun (adgroup:AdGroup) ->             
        let la = fr.EmployeeCount
        let lf = adgroup.Count            
        let p = ((lf |> double) / (la |> double)) * 100.0
        {AdGroup = adgroup; EmployeePercentage = p}
    )

    // convertToCsvFilterResultWithStatistics
    |> (fun (res:list<AdGroupStatistics>) ->
        {Rows = fr.Rows; EmployeeCount = fr.EmployeeCount; AdGroupsWithStatics = res; Filter = fr.Filter}
    )



let oneCompany:CsvFilterResultWithAdGroupStatistics = processFilterToResult csvrows filters.[0]


open PrettyTable

let sortAdGroupStatistics (l:AdGroupStatistics list) = 
    List.sortByDescending (fun (m:AdGroupStatistics) -> m.EmployeePercentage) l

let printHeader (result:CsvFilterResultWithAdGroupStatistics) = 
    let filter = result.Filter

    let headers = ["FilterName";"CompanyAliases";"EmployeeTypes"; "UserCount"]
    let rows = [[filter.FilterName;(String.concat ";" filter.Companies);(String.concat ";" filter.EmployeeTypes);string result.EmployeeCount]]

    prettyTable rows
    |> withHeaders headers
    |> sprintTable

let printGroups filter (result:CsvFilterResultWithAdGroupStatistics) = 
    
    let headers = ["GroupName";"PercetageInGroup";"Members"]
    let rows = 
        seq { 
            for group in 
                result.AdGroupsWithStatics 
                |> List.filter filter  
                |> sortAdGroupStatistics
                do                 
                [group.AdGroup.GroupName; sprintf "%.2f"group.EmployeePercentage; string group.AdGroup.Count]
        }
    if Seq.length rows > 0 then
        prettyTable (Seq.toList rows)
        |> withHeaders headers
        |> horizontalAlignment FsPrettyTable.Types.Left
        |> sprintTable
    else
        ""
    

let wqtod = 
    use file = System.IO.File.CreateText("groups.cvs")

    for filter in filters do 
    
        let results = 
            filter
            |> processFilterToResult csvrows
    
        let ssheader = printHeader results
        let sbody = printGroups (fun (m:AdGroupStatistics) -> m.EmployeePercentage > 50.0) results

        fprintfn file "%s" ssheader
        fprintfn file "%s" sbody
    file.Close
