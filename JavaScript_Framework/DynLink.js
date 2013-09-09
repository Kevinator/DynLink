var DynLinkSocket = null;

var Callbacks = {};
var CallbacksForEvents = {};
var CallbackId = 0;

function dnyLinkStartup(Domain, Action, Parameter, Callback)
{
    dynLinkConnect();
    DynLinkSocket.onopen = function () {
        DynLinkSocket.onopen = undefined;
        dynLinkSend(Domain, Action, Parameter, Callback);
    };
}

function dnyLinkStartupAndBind(Domain, Action, Parameter)
{
    dynLinkConnect();
    DynLinkSocket.onopen = function () {
        dynLinkSend(Domain, Action, Parameter, dnyLinkStartupAutoBind);
    };
}

function dynLinkConnect() {
    DynLinkSocket = new WebSocket(dynLinkConnectionUrl);
    DynLinkSocket.onmessage = dynLinkGot;
}

function dynLinkSend(Domain, Action, Parameter, Callback) {
    CallbackId = CallbackId + 1;
    var myCallbackId = CallbackId;
    Callbacks[myCallbackId] = Callback;

    var request = {};
    request.Id = myCallbackId;
    request.Domain = Domain;
    request.Action = Action;
    request.Parameters = Parameter;
    DynLinkSocket.send(JSON.stringify(request));
}

function dnyLinkStartupAutoBind(ViewModel)
{
    dynLinkBind(ViewModel);
}

function dynLinkBind(ViewModel)
{
    var bindingTargets = document.querySelectorAll('[data-dynlinkbind]');
    for(var i = 0; i < bindingTargets.length; i++)
    {
        var target = bindingTargets[i];
        var custombind = target.dataset['dynlinkbind-custombind'];
        if(custombind != undefined)
        {
            eval(custombind + '(target, ViewModel[target.dataset["dynlinkbind"]]);');
        }
        else
        {
            switch(target.nodeName)
            {
                case 'LABEL':
                    dynLinkPutToLabel(target, ViewModel[target.dataset["dynlinkbind"]]);
                    break;
                case 'TABLE':
                    dynLinkPutToTable(target, ViewModel[target.dataset["dynlinkbind"]]);
            }
        }
    }
}

function dynLinkAddEventCallback(Event, Callback)
{
    CallbacksForEvents[Event] = Callback;
}

function dynLinkGot(e) {
    var Result = JSON.parse(e.data);
    if(Result.Event != undefined && Result.Event != "")
    {
        if(CallbacksForEvents[Result.Event] != undefined)
            CallbacksForEvents[Result.Event](Result.Result);
    }
    else
    {
        if(Callbacks[Result.Id] != undefined)
            Callbacks[Result.Id](Result.Result);  
    }
}

function dynLinkPutToLabel(label, content)
{
    label.textContent = content;
}

function dynLinkPutToTable(table, content)
{
    for(var i = 0; i < table.rows.length; i++)
    {
        var row = table.rows[i];
        if(row.dataset['dynlinkbindHeaderrow'] != 'true')
        {
            table.deleteRow(i);
        }
    }

    var namesOfDisplayed = table.dataset['dynlinkbindProperties'].split(',');

    for(var i = 0; i < content.length; i++)
    {
        table.insertRow(-1);
        var row = table.rows[table.rows.length-1];

        for (var p = 0; p < namesOfDisplayed.length; p++ ) {
            row.insertCell(-1);
            var cell = row.cells[row.cells.length-1];
            cell.textContent = content[i][namesOfDisplayed[p]];
        }
    }
}