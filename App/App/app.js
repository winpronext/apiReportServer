///#source 1 1 D:\Projects\angularapp\App\App\http-auth-interceptor.js
/*Modified for cancel login*/

/*global angular:true, browser:true */

/**
* @license HTTP Auth Interceptor Module for AngularJS
* (c) 2012 Witold Szczerba
* License: MIT
*/
(function () {
    'use strict';

    angular.module('http-auth-interceptor', ['http-auth-interceptor-buffer'])

    .factory('authService', ['$rootScope', 'httpBuffer', function ($rootScope, httpBuffer) {
        return {
            /**
      * call this function to indicate that authentication was successfull and trigger a
      * retry of all deferred requests.
      * @param data an optional argument to pass on to $broadcast which may be useful for
      * example if you need to pass through details of the user that was logged in
      */
            loginConfirmed: function (data, configUpdater) {
                var updater = configUpdater || function (config) { return config; };
                $rootScope.$broadcast('event:auth-loginConfirmed', data);
                httpBuffer.retryAll(updater);
            },
            /**
      * call this function to indicate that authentication was canceled and reject
      * all deferred requests.
      */
            loginCanceled: function(data) {
                $rootScope.$broadcast('event:auth-loginCanceled', data);
                httpBuffer.rejectAll();
            }
        };
    }])

    /**
  * $http interceptor.
  * On 401 response (without 'ignoreAuthModule' option) stores the request
  * and broadcasts 'event:angular-auth-loginRequired'.
  */
    .config(['$httpProvider', function ($httpProvider) {

        var interceptor = ['$rootScope', '$q', 'httpBuffer', function ($rootScope, $q, httpBuffer) {
            var self = {};

            self.response = function(response) {
                if (response.status === 401 && !response.config.ignoreAuthModule) {
                    var deferred = $q.defer();
                    httpBuffer.append(response, deferred);
                    $rootScope.$broadcast('event:auth-loginRequired');
                    return deferred.promise;
                }
                // otherwise, default behaviour
                return response;
            };

            return self;
        }];
        
        $httpProvider.interceptors.push(interceptor);
    }]);

    /**
  * Private module, a utility, required internally by 'http-auth-interceptor'.
  */
    angular.module('http-auth-interceptor-buffer', [])

    .factory('httpBuffer', ['$injector', function ($injector) {
        /** Holds all the requests, so they can be re-requested in future. */
        var buffer = [];

        /** Service initialized later because of circular dependency problem. */
        var $http;

        function retryHttpRequest(config, deferred) {
            function successCallback(response) {
                deferred.resolve(response);
            }
            function errorCallback(response) {
                deferred.reject(response);
            }
            $http = $http || $injector.get('$http');
            $http(config).then(successCallback, errorCallback);
        }

        return {
            /**
      * Appends HTTP request configuration object with deferred response attached to buffer.
      */
            append: function (response, deferred) {
                buffer.push({
                    response: response,
                    deferred: deferred
                });
            },

            /**
      * Retries all the buffered requests clears the buffer.
      */
            retryAll: function (updater) {
                for (var i = 0; i < buffer.length; ++i) {
                    retryHttpRequest(updater(buffer[i].response.config), buffer[i].deferred);
                }
                buffer = [];
            },
            /**
      * Reject all the buffered requests clears the buffer.
      */
            rejectAll: function () {
                for (var i = 0; i < buffer.length; ++i) {
                    buffer[i].deferred.reject(buffer[i].response);
                }
                buffer = [];
            }
        };
    }]);
})();
///#source 1 1 D:\Projects\angularapp\App\App\interceptors.js
(function(angular, toastr) {
    'use strict';

    angular.module('interceptors', [])
        .config(['$httpProvider', function ($httpProvider) {
            $httpProvider.interceptors.push('httpRequestInterceptorIECacheSlayer');
            $httpProvider.interceptors.push('errorHttpInterceptor');
            $httpProvider.interceptors.push('httpAjaxInterceptor');
        }])

        //#region httpRequestInterceptorIECacheSlayer
        // IE 8 cache problem - Request Interceptor - https://github.com/angular/angular.js/issues/1418#issuecomment-11750815
        .factory('httpRequestInterceptorIECacheSlayer', ['$log', function($log) {
            return {
                request: function(config) {
                    if (config.url.indexOf("App/") == -1) {
                        var d = new Date();
                        config.url = config.url + '?cacheSlayer=' + d.getTime();
                    }
                    $log.info('request.url = ' + config.url);

                    return config;
                }
            };
        }])
        //#endregion

        //#region errorHttpInterceptor
        .factory('errorHttpInterceptor', ['$q',
            function($q) {
                return {
                    response: function (response) {
                        if (response.status == 401) {
                            return response;
                        } else if (response.status == 400 && response.data && response.data.message) {
                            toastr.error(response.data.message);
                            return $q.reject(response);
                        } else if (response.status === 0) {
                            toastr.error('Server connection lost');
                            return $q.reject(response);
                        } else if (response.status >= 400 && response.status < 500) {
                            toastr.error('Server was unable to find' +
                                ' what you were looking for... Sorry!!');
                            return $q.reject(response);
                        }
                        return response;
                    }
                };
            }])
        //#endregion
    
        //#region httpAjaxInterceptor
        .factory('httpAjaxInterceptor', ['$q', '$location', '$rootScope', '$timeout',
            function($q, $location, $rootScope, $timeout) {
                var queue = [];
                var timerPromise = null;
                var timerPromiseHide = null;

                function processRequest() {
                    queue.push({});
                    if (queue.length == 1) {
                        timerPromise = $timeout(function() {
                            if (queue.length) {
                                $rootScope.$broadcast('event:ajax-show');
                            }
                        }, 500);
                    }
                }

                function processResponse() {
                    queue.pop();
                    if (queue.length == 0) {
                        //Since we don't know if another XHR request will be made, pause before
                        //hiding the overlay. If another XHR request comes in then the overlay
                        //will stay visible which prevents a flicker
                        timerPromiseHide = $timeout(function() {
                            //Make sure queue is still 0 since a new XHR request may have come in
                            //while timer was running
                            if (queue.length == 0) {
                                $rootScope.$broadcast('event:ajax-hide');
                                if (timerPromiseHide) $timeout.cancel(timerPromiseHide);
                            }
                        }, 500);
                    }
                }

                return {
                    request: function(config) {
                        processRequest();
                        return config || $q.when(config);
                    },
                    response: function(response) {
                        processResponse();
                        return response || $q.when(response);
                    },
                    responseError: function(rejection) {
                        processResponse();
                        return rejection || $q.when(rejection);
                    }
                };
            }]);
        //#endregion

})(angular, toastr);


///#source 1 1 D:\Projects\angularapp\App\App\services.js
(function (angular, $) {
    'use strict';

    var services = angular.module('services', []);
    
    //#region $auth
    services.factory('$auth', ['$q', '$http', '$path', function ($q, $http, $path) {
        
        var profileUrl = $path('api/Account/Profile');
        var tokenUrl = $path('api/Account/Token');

        function setupAuth(accessToken, remember) {
            var header = 'Bearer ' + accessToken;
            delete $http.defaults.headers.common['Authorization'];
            $http.defaults.headers.common['Authorization'] = header;
            sessionStorage['accessToken'] = accessToken;
            if (remember) {
                localStorage['accessToken'] = accessToken;
            }
            return header;
        }
        
        function clearAuth() {
            sessionStorage.removeItem('accessToken');
            localStorage.removeItem('accessToken');
            delete $http.defaults.headers.common['Authorization'];
        }

        var self = {};
        var userName;

        self.getUserName = function() {
            return userName;
        };

        self.isAuthenticated = function() {
            return userName && userName.length;
        };

        self.loadSaved = function() {
            var deferred = $q.defer();
            var accessToken = sessionStorage['accessToken'] || localStorage['accessToken'];
            if (accessToken) {
                setupAuth(accessToken);
                $http.get(profileUrl, { ignoreAuthModule: true })
                    .success(function(data) {
                        userName = data.userName;
                        deferred.resolve({
                            userName: data.userName
                        });
                    })
                    .error(function() {
                        clearAuth();
                        deferred.reject();
                    });
            } else {
                deferred.reject();
            }

            return deferred.promise;
        };

        self.login = function(user, passw, rememberMe) {
            var deferred = $q.defer();
            $http.post(tokenUrl, { userName: user, password: passw })
                .success(function (data) {
                    var header = setupAuth(data.accessToken, rememberMe);
                    deferred.resolve({
                        userName: data.userName,
                        Authorization: header
                    });
                })
                .error(function() {
                    deferred.reject();
                });

            return deferred.promise;
        };

        return self;
    }]);
    //#endregion

    //#region $safeApply
    services.factory('$safeApply', function () {
        return function($scope, fn) {
            var phase = $scope.$root ? $scope.$root.$$phase : null;
            if (phase == '$apply' || phase == '$digest') {
                if (fn) {
                    $scope.$eval(fn);
                }
            } else {
                if (fn) {
                    $scope.$apply(fn);
                } else {
                    $scope.$apply();
                }
            }
        };
    });
    //#endregion
    
    //#region $path
    services.factory('$path', function () {
        var uri = window.location.href;
        var ind = uri.indexOf('#');

        var baseUrl = uri;

        if (ind > 0) {
            baseUrl = uri.substr(0, ind);
        }

        if (baseUrl[baseUrl.length - 1] != '/') {
            baseUrl = baseUrl + '/';
        }

        return function(url) {
            return baseUrl + url;
        };
    });
    //#endregion
    
    //#region $signalR
    services.factory('$signalR', ['$rootScope', function ($rootScope) {
        var self = $rootScope.$new();
        
        //Log4Net.SignalR
        var log4Net = $.connection.signalrAppenderHub;
        log4Net.client.onLoggedEvent = function (loggedEvent) {
            self.$emit('loggedEvent', loggedEvent);
        };


        $.connection.hub.start();
        return self;
    }]);
    //#endregion

})(window.angular, window.jQuery);
///#source 1 1 D:\Projects\angularapp\App\App\resources.js
(function (angular) {
    'use strict';

    var resources = angular.module('resources', []);

    //#region TodoItem
    resources.factory('TodoItem', ['$resource', '$path', function ($resource, $path) {
        return $resource($path('api/TodoItem/:id'), { id: '@Id' }, {
            update: { method: 'PUT' }
        });
    }]);
    //#endregion

    //#region TodoList
    resources.factory('TodoList', ['$resource', '$path', function ($resource, $path) {
        return $resource($path('api/TodoList/:id/:action'), { id: '@Id' }, {
            todos: { method: 'GET', isArray: true, params: { action: 'Todos' } }
        });
    }]);
    //#endregion
})(window.angular);

///#source 1 1 D:\Projects\angularapp\App\App\directives.js
(function (angular, modernizr) {
    'use strict';

    var directives = angular.module('directives', []);

    //#region busyIndicator
    directives.directive('busyIndicator', ['$rootScope', function ($rootScope) {
        return {
            restrict: 'A',
            link: function (scope, elem) {
                var innerHtml = null;
                
                $rootScope.$on('event:ajax-show', function () {
                    if (innerHtml) return;
                    innerHtml = elem[0].innerHTML;
                    elem[0].innerHTML = 'Loading...';
                });
                
                $rootScope.$on('event:ajax-hide', function () {
                    elem[0].innerHTML = innerHtml || elem[0].innerHTML;
                    innerHtml = null;
                });
            }
        };
    }]);
    //#endregion

    //#region authDialog
    directives.directive('authDialog', function () {
        var instances = 0;
        
        return {
            restrict: 'A',
            link: function (scope, elem) {
                var $elem = $(elem);
                
                $elem.modal({
                    backdrop: false,
                    keyboard: false,
                    show: false
                });

                function showRequest() {
                    if (instances == 0) {
                        $elem.modal('show');
                    }
                    instances++;
                }
                
                function hideRequest() {
                    if (instances > 0) {
                        $elem.modal('hide');
                    }
                }

                function onHide() {
                    instances--;
                    if (instances > 0) {
                        instances = 0;
                        showRequest();
                    }
                }

                $(elem).on('hidden.bs.modal', onHide);
                scope.$on('event:auth-loginRequired', showRequest);
                scope.$on('event:auth-loginConfirmed', hideRequest);
                scope.$on('event:auth-loginCanceled', hideRequest);
            }
        };
    });
    //#endregion

    //#region Ng directives
    /*  We extend Angular with custom data bindings written as Ng directives */
    directives.directive('onFocus', function () {
        return {
            restrict: 'A',
            link: function (scope, elm, attrs) {
                elm.bind('focus', function () {
                    scope.$apply(attrs.onFocus);
                });
            }
        };
    })
        .directive('onBlur', function () {
            return {
                restrict: 'A',
                link: function (scope, elm, attrs) {
                    elm.bind('blur', function () {
                        scope.$apply(attrs.onBlur);
                    });
                }
            };
        })
        .directive('onEnter', function () {
            return function (scope, element, attrs) {
                element.bind("keydown keypress", function (event) {
                    if (event.which === 13) {
                        scope.$apply(function () {
                            scope.$eval(attrs.onEnter);
                        });

                        event.preventDefault();
                    }
                });
            };
        })
        .directive('selectedWhen', function () {
            return function (scope, elm, attrs) {
                scope.$watch(attrs.selectedWhen, function (shouldBeSelected) {
                    if (shouldBeSelected) {
                        elm.select();
                    }
                });
            };
        });
    if (!modernizr.input.placeholder) {
        // this browser does not support HTML5 placeholders
        // see http://stackoverflow.com/questions/14777841/angularjs-inputplaceholder-directive-breaking-with-ng-model
        directives.directive('placeholder', function () {
            return {
                restrict: 'A',
                require: 'ngModel',
                link: function (scope, element, attr, ctrl) {

                    var value;

                    var placeholder = function () {
                        element.val(attr.placeholder);
                    };
                    var unplaceholder = function () {
                        element.val('');
                    };

                    scope.$watch(attr.ngModel, function (val) {
                        value = val || '';
                    });

                    element.bind('focus', function () {
                        if (value == '') unplaceholder();
                    });

                    element.bind('blur', function () {
                        if (element.val() == '') placeholder();
                    });

                    ctrl.$formatters.unshift(function (val) {
                        if (!val) {
                            placeholder();
                            value = '';
                            return attr.placeholder;
                        }
                        return val;
                    });
                }
            };
        });
    }
    //#endregion 
    
})(window.angular, Modernizr);

///#source 1 1 D:\Projects\angularapp\App\App\controllers.js
(function (angular) {
    'use strict';

    var controllers = angular.module('controllers', []);

    //#region HomeCtrl
    controllers.controller('HomeCtrl', ['$scope', function ($scope) {
        $scope.title = 'Home';
    }]);
    //#endregion

    //#region TodosCtrl
    controllers.controller('TodosCtrl', ['$scope', '$safeApply', 'TodoList', 'TodoItem',
        function ($scope, $safeApply, TodoList, TodoItem) {
            $scope.newTodoListName = '';
            
            $scope.todoLists = TodoList.query();

            $scope.expand = function (todoList) {
                if (todoList.todoItems) return;
                todoList.todoItems = TodoList.todos({ id: todoList.id });
            };

            $scope.addTodoList = function () {
                var todoList = new TodoList();
                todoList.title = $scope.newTodoListName;
                todoList.$save(function (data) {
                    $safeApply($scope, function () {
                        $scope.todoLists.push(data);
                        $scope.newTodoListName = '';
                    });
                });
            };
            
            $scope.removeTodoList = function (todoList) {
                var idTodoList = todoList.id;
                TodoList.remove(todoList, function () {
                    for (var i = 0; i < $scope.todoLists.length; i++) {
                        if ($scope.todoLists[i].id == idTodoList) {
                            $scope.todoLists.splice(i, 1);
                            break;
                        }
                    }
                });
            };
            
            $scope.addTodoItem = function (todoList) {
                var todoItem = new TodoItem();
                todoItem.title = todoList.newTodoItemName;
                todoItem.todoListId = todoList.id;

                todoItem.$save(function(data) {
                    $safeApply($scope, function () {
                        todoList.todoItems.push(data);
                        todoList.newTodoItemName = '';
                    });
                });
            };

            $scope.saveTodoItem = function(todoItem) {
                TodoItem.update(todoItem);
            };

            $scope.removeTodoItem = function(todoList, todo) {
                var idTodo = todo.id;
                TodoItem.remove(todo, function() {
                    for (var i = 0; i < todoList.todoItems.length; i++) {
                        if (todoList.todoItems[i].id == idTodo) {
                            todoList.todoItems.splice(i, 1);
                            break;
                        }
                    }
                });
            };
        }]);
    //#endregion
    
    //#region AboutCtrl
    controllers.controller('AboutCtrl', ['$scope', function ($scope) {
        $scope.title = 'About';
    }]);
    //#endregion
    
    //#region SettingsCtrl
    controllers.controller('SettingsCtrl', ['$scope', function ($scope) {
        $scope.title = 'Settings';
    }]);
    //#endregion

    //#region NavCtrl
    controllers.controller('NavCtrl', ['$scope', '$location',
        function ($scope, $location) {
            $scope.getClass = function (button) {
                var path = $location.path();
                if (path.indexOf(button) === 0) {
                    return 'active';
                } else {
                    return '';
                }
            };
        }]);
    //#endregion

    //#region LoginCtrl
    controllers.controller('LoginCtrl', ['$scope', '$safeApply', 'authService', '$rootScope', '$auth',
        function($scope, $safeApply, authService, $rootScope, $auth) {
            $scope.userName = '';
            $scope.password = '';
            $scope.rememberMe = false;

            $scope.signIn = function() {
                $auth.login($scope.userName, $scope.password, $scope.rememberMe)
                    .then(function (data) {
                        $safeApply($scope, function() {
                            $rootScope.userName = data.userName;
                        });
                        authService.loginConfirmed({
                                user: data.userName
                            },
                            function(config) {
                                config.headers.Authorization = data.Authorization;
                                return config;
                            });
                    });
            };

            $scope.cancel = function() {
                authService.loginCanceled();
            };

        }]);
    //#endregion
    
    //#region LogsCtrl
    controllers.controller('LogsCtrl', ['$scope', '$safeApply', '$signalR',
        function ($scope, $safeApply, $signalR) {
            $scope.logs = [];

            var logsType = [];
            logsType['FATAL'] = 'alert alert-error repeat-item';
            logsType['ERROR'] = 'alert alert-error repeat-item';
            logsType['WARN'] = 'alert alert-info repeat-item';
            logsType['INFO'] = 'alert alert-success repeat-item';
            logsType['DEBUG'] = 'alert alert-info repeat-item';

            $signalR.$on('loggedEvent', function (e, loggedEvent) {
                $safeApply($scope, function() {
                    loggedEvent.class = logsType[loggedEvent.Level];
                    $scope.logs.splice(0, 0, loggedEvent);
                });
            });

            $scope.clearLogs = function () {
                $scope.logs.length = 0;
            };
            
        }]);
    //#endregion

})(window.angular);
///#source 1 1 D:\Projects\angularapp\App\App\main.js
/* main: startup script creates the 'app' module */

(function (window, angular, toastr) {
    'use strict';
    
    //#region configure toastr
    toastr.options.closeButton = true;
    toastr.options.newestOnTop = false;
    toastr.options.positionClass = 'toast-bottom-right';
    //#endregion

    // 'app' is the one Angular (Ng) module in this app
    // 'app' module is in global namespace
    window.app = angular.module('app', [
        //ng modules
        'ngRoute',
        'ngAnimate',
        'ngResource',
        //custom modules
        'services',
        'directives',
        'resources',
        'controllers',
        'http-auth-interceptor',
        'interceptors'
    ]);

    var exceptionHandler = function (e) {
        toastr.error(e.message || e);
    };

    // Learn about Angular dependency injection in this video
    // http://www.youtube.com/watch?feature=player_embedded&v=1CpiB3Wk25U#t=2253s
    app.value('$exceptionHandler', exceptionHandler);

    //#region Configure routes
    app.config(['$routeProvider', '$locationProvider', 
        function($routeProvider, $locationProvider) {
            $routeProvider.
                when('/home', { templateUrl: 'App/views/home.html', controller: 'HomeCtrl' }).
                when('/todos',
                    {
                        templateUrl: 'App/views/todos.html',
                        controller: 'TodosCtrl',
                        resolve: {
                            authentication: ['$http', function ($http) {
                                return $http.get('api/Account/Ping');
                            }]
                        }
                    }).
                when('/about', { templateUrl: 'App/views/about.html', controller: 'AboutCtrl' }).
                when('/settings',
                    {
                        templateUrl: 'App/views/settings.html',
                        controller: 'SettingsCtrl',
                        resolve: {
                            authentication: ['$http', function($http) {
                                return $http.get('api/Account/Ping');
                            }]
                        }
                    }).
                when('/logs', { templateUrl: 'App/views/logs.html', controller: 'LogsCtrl' }).
                otherwise({ redirectTo: '/home' });

            $locationProvider.html5Mode(false).hashPrefix('!');
        }]);
    //#endregion

    app.run(['$rootScope', '$location', '$window', '$auth',
        function($rootScope, $location, $window, $auth) {
            $rootScope.today = new Date();

            $auth.loadSaved().then(function (data) {
                $rootScope.userName = data.userName;
            });

            $rootScope.$on('$routeChangeError', function(event, current, previous) {
                if (previous) {
                    $location.path(previous.originalPath);
                } else {
                    $window.location.reload();
                }
            });
        }]);

})(window, angular, toastr);
