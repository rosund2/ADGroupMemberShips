#r @"nuget: System.DirectoryServices"
#r @"System.Security.AccessControl"

open System.Collections
open System.DirectoryServices
open System.Collections.Generic

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


