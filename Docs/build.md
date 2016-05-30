Building the Kit
================

Dependencies
------------

### Unity
Unity is required to build Unity libs.

### Mono
Mono is required to build assemblies and for NUnit.

```
brew install mono
```

### NUnit
The gulp NUnit runner has a bug that expects to find the executable with a `.exe` suffix. 

Link to the one installed by Mono into `/usr/local/bin`.

```
cd /usr/local/bin
sudo ln -s nunit-console nunit-console.exe
```

### Node.js
Required for package management.

```
brew install node
```

### Doxygen
Required to build docs. See `Doxyfile`.

* Graphviz for `dot` to generate doc diagrams
* Qt for `qhelpgenerator` to generate Qt docs

```
brew install doxygen graphviz qt
```

Compiling
---------

### Install dependencies
Install dependencies with npm.

```
npm install
```

The NPM post-install script will also start a build.

### Build tasks

Run tasks using gulp.

```
gulp build
```

See `gulpfile.ts` for details.