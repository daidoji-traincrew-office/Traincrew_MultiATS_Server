env "local"{
   src = "file://schema.sql" 
   
   url = "postgres://postgres:postgres@192.168.0.15:5432/postgres?search_path=public&sslmode=disable"
   
   dev = "postgres://postgres:postgres@192.168.0.15:5432/postgres?search_path=dev&sslmode=disable"
}

env "dev"{
   src = "file://schema.sql" 
   
   url = "postgres://postgres:postgres@10.13.13.7:5433/postgres?search_path=public&sslmode=disable"
   
   dev = "postgres://postgres:postgres@192.168.0.15:5432/postgres?search_path=dev&sslmode=disable"
}
