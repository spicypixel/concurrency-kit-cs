var gulp = require("gulp");
var gutil = require("gulp-util");
var msbuild = require("gulp-msbuild");
var nunit = require("gulp-nunit-runner");
var del = require("del");
var shell = require("gulp-shell");
var spawn = require("child_process").spawn;
var exec = require('child_process').exec;
var path = require("path");
var pathExists = require("path-exists");
var mkdirp = require('mkdirp');
var flatten = require("gulp-flatten");

// Default task to run continuous integration
gulp.task("default", ["build"]);

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
gulp.task("test", () => buildCode().then(() => test()));

// Install
gulp.task("install", () => build());
gulp.task("install-monodocs", () => installMonoDocs());

function clean() {
  gutil.log(gutil.colors.cyan("Cleaning ..."));
  return del(["Source/**/bin/*", "Source/**/obj/*", 
    "Doxygen/html", "Doxygen/xml", "Doxygen/qt",
    "MonoDoc/xml", "MonoDoc/assemble", "MonoDoc/html"]);
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
  gutil.log(gutil.colors.cyan("Installing doxygen theme ..."));
  
  return spawnAsync("npm", ["install"], 
    {cwd: path.join(__dirname, "Doxygen", "theme", "bootstrap")});
}

// Build Doxygen
function buildDoxygen() {
  gutil.log(gutil.colors.cyan("Building Doxygen docs ..."));
  return spawnAsync("doxygen");    
}

// Update MonoDoc external docs
function updateMonoDoc() {  
  var assemblies = [
    "System.Threading", 
    "SpicyPixel.Threading",
    "SpicyPixel.Threading.Unity"];
    
  var assemblyPaths = [];
  assemblies.forEach(a => {
    assemblyPaths = assemblyPaths.concat(
      path.join(__dirname, "Source", a,
      "bin", "Release", a + ".dll"));
  });
  
  var xmlParams = [];
  assemblies.forEach(a => {
    xmlParams = xmlParams.concat("-i");
    xmlParams = xmlParams.concat(
      path.join(__dirname, "Source", a,
      "bin", "Release", a + ".xml"));
  });
  
  return spawnAsync("mdoc", [
    "update",
    "-out:MonoDoc/xml",
  ].concat(xmlParams).concat(assemblyPaths));
}

// Merge Doxygen to MonoDoc
function mergeDoxygenToMonoDoc() {
  gutil.log(gutil.colors.cyan("Merging docs ..."));
  var appDir = require.resolve("doxygentoecma")
    .match(/.*\/node_modules\/[^/]+\//)[0];
  var doxygentoecmaPath = path.join(appDir, "bin", "Release", "doxygentoecma.exe");

  return spawnAsync("mono", [doxygentoecmaPath,
    path.join(__dirname, "Doxygen/xml"), 
    path.join(__dirname, "MonoDoc/xml")]);
}

function assembleMonoDoc() {
  gutil.log(gutil.colors.cyan("Assembling MonoDocs ..."));
  
  var assembleDir = path.join(__dirname,"MonoDoc/assemble");
  mkdirp.sync(assembleDir);
  var prefix = path.join(assembleDir, "SpicyPixel.ConcurrencyKit");
  return spawnAsync("mdoc", [
    "assemble", "-o", prefix, "MonoDoc/xml"
  ]);
}

function installMonoDocs() {
  return new Promise((resolve, reject) => {
    getMonoDocInstallPath((path) => {
      gulp
        .src(["MonoDoc/assemble/*", "MonoDoc/*.source"])
        .pipe(flatten())
        .pipe(gulp.dest(path))
        .on("end", resolve)
        .on("error", reject);
    });
  });
}

// Build docs
function buildDocs() {
  return installDoxygenTheme()
    .then(() => buildDoxygen())
    .then(() => updateMonoDoc())
    // .then(() => mergeDoxygenToMonoDoc())
    .then(() => assembleMonoDoc());
}

// Test with NUnit
function test() {
  return gulp
    .src(["**/bin/**/*Test.dll"], { read: false })
    .pipe(nunit({
      executable: "/usr/local/bin",
      teamcity: false
    }));
}

function spawnAsync(command) {
  return spawnAsync(command, undefined);  
}

function spawnAsync(command, args) {
  return spawnAsync(command, args, undefined);
}

function spawnAsync(command, args, properties) {
  return new Promise((resolve, reject) => {
    var proc = spawn(command, args, properties);
    proc.stdout.setEncoding("utf8");
    proc.stderr.setEncoding("utf8");
    proc.stdout.on("data", data => gutil.log(data));
    proc.stderr.on("data", data => gutil.colors.red(gutil.log(data)));
    proc.on("exit", code => code == 0 ? resolve() : reject(code));
  });
}

function execute(command, callback) {
    exec(command, function(error, stdout, stderr) { 
      callback(stdout); 
    });
};

function getMonoDocInstallPath(callback)
{
  callback("/Library/Frameworks/Mono.framework/External/monodoc/");
  // execute("pkg-config monodoc --variable=sourcesdir", callback);
}