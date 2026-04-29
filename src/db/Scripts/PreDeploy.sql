/*
    Master PreDeploy script — included by sqlproj as the single PreDeploy.
    Runs before sqlpackage applies the schema diff. Each :r included file
    must be idempotent.
*/
:r .\PreDeploy\01-recreate-fn-tohiragana.sql
