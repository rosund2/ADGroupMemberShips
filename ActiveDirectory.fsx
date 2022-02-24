#r @"nuget: System.DirectoryServices"
#r @"System.Security.AccessControl"

open System.Collections
open System.DirectoryServices
open System.Collections.Generic


// let adsrv = new DirectoryEntry ( @"LDAP://lyse.no/DC=lyse,DC=no")
// adsrv.AuthenticationType = AuthenticationTypes.ReadonlyServer

// let userQuery = @"(&(objectCategory=person)(objectClass=user)(sAMAccountName=*)(!useraccountcontrol:1.2.840.113556.1.4.803:=2))"

let propertyNames (ds:DirectoryEntry) q =     
    let ds = new DirectorySearcher(ds)
    ds.Filter <- q
    let p = ds.FindOne()
    p.Properties.PropertyNames |> Seq.cast<string> |> Seq.toList

let memoize f =
    let memory = new Dictionary<string, list<string>>()
    
    let fn = (fun x -> 
                match memory.TryGetValue(x) with 
                | true, value -> value
                | false,_ -> 
                    let r = f x                
                    memory.Add(x, r)
                    r
            )
    fn
    


let convertDictionaryToMap s (d:IDictionary) =     

    Seq.fold (fun (x:Map<string,list<string>>) y ->         
        let o = d.Item(y)        
        if o <> null then                  
            let q =  (o :?> ResultPropertyValueCollection) |> Seq.cast<obj> |> Seq.toList |> List.map string
            Map.add y q x
        else
            x                
    ) Map.empty s

let ldapSearcher (de:DirectoryEntry) (attrs:list<string> option) (f:(DirectorySearcher -> unit) option) (filter:string)  =     
    
    let s = new DirectorySearcher(de, filter)
    
    match f with 
    | Some f -> f s |> ignore
    | None -> ()
    

    let props = 
        if Option.isSome attrs then
            Option.defaultValue [] attrs
        else
            let memoizedPropNames = memoize (propertyNames de)
            memoizedPropNames filter

    props 
    |> Seq.iter (fun attr -> s.PropertiesToLoad.Add(attr) |> ignore)
        
    let sres = s.FindAll() 


    sres 
    |> Seq.cast<SearchResult> 
    |> Seq.map (fun x -> 
        convertDictionaryToMap props x.Properties        
    ) 
    |> Seq.toList



// let userAttributes = Some ["samaccountname";"memberof";"company";"department";"title";"userprincipalname";"employeetype";"employeenumber"]
// let ldapSearcherWithDe = ldapSearcher adsrv

// let queryResult = ldapSearcherWithDe userAttributes None userQuery

// printfn "Found %i employees" queryResult.Length

// let firstAttrToString key  (m:Map<string,list<string>>) = 
//     match m.TryFind key  with 
//     | Some a -> List.head a
//     | None -> "null"  

// let onlyWithGroups = queryResult |> List.filter (fun x -> x.ContainsKey("memberof") && x.ContainsKey("company") ) 

// let sq =     
//     seq {
//         for entry in onlyWithGroups do              
//             yield! seq {for group in entry.["memberof"] -> 
//                         seq {
//                             group; 
//                             (firstAttrToString "userprincipalname" entry);                            
//                             (firstAttrToString "company" entry);
//                             (firstAttrToString "title" entry);
//                             (firstAttrToString "employeetype" entry)
//                             (firstAttrToString "employeenumber" entry)
//                             }}
//     }

// let wtof = 
//     use file = System.IO.File.CreateText("Atest2.cvs")
//     for i in sq do 
//         fprintfn file "%s" (String.concat ";" i)
//     file.Close


