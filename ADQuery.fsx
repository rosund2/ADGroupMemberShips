#r @"nuget: System.DirectoryServices"

open System
open System.Collections
open System.DirectoryServices
open System.Collections.Generic

let domain = @"LDAP://x500.bund.de/o=bund,c=DE"

let userFilter = @"(&(objectCategory=User)(objectClass=person))"
let userAttributes = ["samAccountName";"memberOf"]

let groupFilter = @"(&(objectCategory=Group))"

// (&(objectCategory=Person)(sAMAccountName=*)(memberOf=cn=CaptainPlanet,ou=users,dc=company,dc=com))

let dirsearcher = new DirectoryEntry(domain)

dirsearcher.AuthenticationType <- AuthenticationTypes.ReadonlyServer

let  s = new DirectorySearcher(dirsearcher, @"(objectClass=*)")


let ldapSearcher (de:DirectoryEntry) (attrs:list<string> option) (filter:string)  =     
    try 
        let s = new DirectorySearcher(de, filter)
        
        attrs 
        |> Option.iter
            (fun attrs -> 
                List.iter (fun attr -> s.PropertiesToLoad.Add(attr) |> ignore) attrs)
            

        let sres = s.FindAll() 
        let q = sres |> Seq.cast<SearchResult>
        Ok q
    with
    | :? NotSupportedException as ex -> ex.Message |> Error
    | :? InvalidOperationException as ex -> ex.Message |> Error
    
let ldapSearcherWithDe = ldapSearcher dirsearcher None

let myres = ldapSearcherWithDe @"(objectClass=*)" 


// https://www.codemag.com/article/1312041/Using-Active-Directory-in-.NET
// This might be usefull to discover domain server
let rootDe = new DirectoryEntry ( @"LDAP://RootDSE")





//
// The point of the algo is to do a couple of queries 
// concat a maplike thingy // group-info; member-info
// We first need to get all the user-info
// The we need to get the info one the groups 
// 



// 
// Returns a SearchResultCollection of SearchResults
//
// let srescol = s.FindAll()


//for sresult in srescol do 
    
    

//let cl = srescol |> Seq.cast<SearchResult> |> List.ofSeq

//let sq = [for i in srescol do yield i]

//let q = sq |> List.map (fun entry -> entry.Properties |> Seq.cast<DictionaryEntry>) 

//let fl = List.head q

//let propsToString (props:seq<DictionaryEntry>) : string =     
//    let sb = new System.Text.StringBuilder()
//    printfn "props: %A" props
//    for i in props do
//        let s = sprintf "%A" (i.Key,i.Value) 
//        sb.Append(s) |> ignore    
//    sb.ToString()    


//q |> List.iter (fun entry -> 
//    printfn "asdasd"
//    entry |> propsToString |> printfn "%s" |> ignore)

//printfn "%A" sq.Head.Properties.Values

//for i in res do         
//    printfn "Somethhing: %s" (i.GetDirectoryEntry().Name)
//with 
//    | :? System.Exception as ex -> printfn "%s" ex.Message