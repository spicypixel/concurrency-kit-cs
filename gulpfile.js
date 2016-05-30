// Transpile on demand and execute
eval(
  require("typescript").transpile(
    require("fs").readFileSync("./gulpfile.ts").toString())
);