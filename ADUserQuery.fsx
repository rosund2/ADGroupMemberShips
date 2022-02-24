#r @"nuget: System.DirectoryServices"
#r @"System.Security.AccessControl"

open System.DirectoryServices


#load "ActiveDirectory.fsx"
open ActiveDirectory

#load "UserQueryConfig.fsx"
open UserQueryConfig

let userAttributes = Some ["samaccountname";"memberof";"company";"department";"title";"userprincipalname";"employeetype";"employeenumber"]
let ldapSearcherWithDe = ldapSearcher adsrv

let queryResult = ldapSearcherWithDe userAttributes None userQuery

printfn "Found %i employees" queryResult.Length

let firstAttrToString key  (m:Map<string,list<string>>) = 
    match m.TryFind key  with 
    | Some a -> List.head a
    | None -> "null"  

let onlyWithGroups = queryResult |> List.filter (fun x -> x.ContainsKey("memberof") && x.ContainsKey("company") ) 

let sq =     
    seq {
        for entry in onlyWithGroups do              
            yield! seq {for group in entry.["memberof"] -> 
                        seq {
                            group; 
                            (firstAttrToString "userprincipalname" entry);                            
                            (firstAttrToString "company" entry);
                            (firstAttrToString "title" entry);
                            (firstAttrToString "employeetype" entry)
                            (firstAttrToString "employeenumber" entry)
                            }}
    }

let wtof = 
    use file = System.IO.File.CreateText("Atest.cvs")
    for i in sq do 
        fprintfn file "%s" (String.concat ";" i)
    file.Close


