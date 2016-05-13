var gulp = require("gulp");
var gutil = require("gulp-util");
var msbuild = require("gulp-msbuild");
var nunit = require("gulp-nunit-runner");
var del = require("del");
var spawn = require("child_process").spawn;
var path = require("path");
var pathExists = require("path-exists");

// Default task to run continuous integration
gulp.task("default", ["ci"]);

// Continuous integration runs the test suite
gulp.task("ci", ["test"]);

// Rebuild is a clean followed by build
gulp.task("rebuild", () => clean().then(() => build()));

// Clean removes build artifacts
gulp.task("clean", () => clean());

// Build code and docs
gulp.task("build", () => buildCode().then(() => buildDocs()));

// Build docs
gulp.task("docs", () => buildDocs());

// Test with NUnit
gulp.task("test", () => build().then(() => test()));

// Install
gulp.task("install", () => build());

function clean() {
  gutil.log(gutil.colors.cyan("Cleaning ..."));
  return del(["Source/**/bin/*", "Source/**/obj/*", "Doxygen/html"]);
}

function build() {
  return buildCode().then(() => buildDocs());
}

// Build code
function buildCode() {
  gutil.log(gutil.colors.cyan("Bulding code ..."));
  var assetsDir = path.join(__dirname, "..", "..", "Assets");
  return pathExists(assetsDir).then(exists => {
    gutil.log("pathExists: ", assetsDir, " = ", exists);
    return new Promise((resolve, reject) => {      
      gulp
        .src("Source/**/*.sln")
        .pipe(msbuild({
            properties: {
              UnityEnginePath: "/Applications/Unity/Unity.app/Contents/Frameworks/Managed/UnityEngine.dll"
            },
            toolsVersion: 12.0,
            targets: ["Build"],
            errorOnFail: true,
            stdout: true
        })).on("end", resolve)
        .on("error", reject);
    });
  });
}

// Install node dependencies
function installDoxygenTheme() {
  return new Promise((resolve, reject) => {
    gutil.log(gutil.colors.cyan("Installing doxygen theme ..."));
    
    var npm = spawn("npm", ["install"], 
      {cwd: path.join(__dirname, "Doxygen", "theme", "bootstrap")});
      
    npm.stdout.setEncoding("utf8");
    npm.stderr.setEncoding("utf8");
    npm.stdout.on("data", data => gutil.log(data));
    npm.stderr.on("data", data => gutil.colors.red(gutil.log(data)));
    npm.on("exit", code => code == 0 ? resolve() : reject(code));
  });
}

// Build docs
function buildDocs() {
  return installDoxygenTheme().then(() => {
    return new Promise((resolve, reject) => {
      gutil.log(gutil.colors.cyan("Building docs ..."));
      var doxygen = spawn("doxygen");
      doxygen.stdout.setEncoding("utf8");
      doxygen.stderr.setEncoding("utf8");
      doxygen.stdout.on("data", data => gutil.log(data));
      doxygen.stderr.on("data", data => gutil.colors.red(gutil.log(data)));
      doxygen.on("exit", code => code == 0 ? resolve() : reject(code));    
    });
  });
}

function test() {
  return gulp
    .src(["**/bin/**/*Test.dll"], { read: false })
    .pipe(nunit({
      executable: "/usr/local/bin",
      teamcity: false
    }));
}