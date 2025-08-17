  $(document).ready(function(){
        
    $( "#sign" ).click(function() {
      if ($( "#sign" ).attr('name')=='login') {
        event.preventDefault();
        $( "#mpopup" ).show();
      }
       else logout()
    })

    $( "#close" ).click(function() {
      event.preventDefault();
      $( "#mpopup" ).hide();
    })
  
    $( "#btnlogin" ).click(function() {
      //M.toast({html: 'Пожалуйста, заполните содержание', classes: '#ef5350 red lighten-1 rounded'});
      event.preventDefault();
      if ($( "#pass" ).val()!=''){
       // $( "#btnl" ).submit();
        let tosend = $( "#pass" ).val();
        let logins = $( "#logins" ).val();
        $.get( "https://platoniks.ru/sso/auth", {pin: tosend} )
        .done(function( data ) {
          console.log( "Data Loaded: " + data );
            if (data=='ok'){
            M.toast({html: 'Авторизация - успешно!', classes: '#26a69a teal lighten-1 rounded'});
            reload(true);
          } else {
            M.toast({html: 'Неверный логин! Повторите попытку!', classes: '#ef5350 red lighten-1 rounded'});
            $( "#pass" ).val('');
          }
        });
        //console.log( "Handler for .click() called." );
      } else {
        M.toast({html: 'Пожалуйста, введите логин!', classes: '#ef5350 red lighten-1 rounded'});
      }    
    });

    
    function logout(){
      M.toast({html: 'Выход из аккаунта', classes: '#ef5350 red lighten-1 rounded'});
      $.get( "/logout");
      reload(true);
    }

    function reload(p){
      if (p) setTimeout(reload, 50);
      else location.reload();
    }
    
    var topic = []
    var less = []




/*
    $('#idclass').change(function(){
      hide_elem()
      console.log($('#idclass').val());
      $.get( "/getless", { id:$('#idclass').val()} ).done(function( data ) {
        //console.log(data);
        if (typeof data=='string') {
          data = JSON.parse(data)
        }
        //
        console.log(typeof data);
       // console.log(data.topics[0]);
       $('#idtopic').html("")
       $('#idtopic').append($('<option>', {
        value: -1,
        text: "Темы",
        disabled:1,
        selected:1
      }));
       data.topics.forEach(element => {
        console.log(element);
        topic[element.id] = element
      
        $('#idtopic').append($('<option>', {
          value: element.id,
          text: element.title
        }));
        
        });
        $('#idtopic').formSelect();
        $('#idtopicdiv').show()
       // topic = data
        //#idless
      })

      $.get( "/getgroup", { id:$('#idclass').val()} ).done(function( data ) {
        //console.log(data);
        if (typeof data=='string') {
          data = JSON.parse(data)
        }
        //
        console.log(typeof data);
       // console.log(data.topics[0]);
       $('#idgr').html("")
       $('#idgr').append($('<option>', {
        value: -1,
        text: "Группа",
        disabled:1,
        selected:1
      }));
       data.forEach(element => {
        console.log(element);
      
        $('#idgr').append($('<option>', {
          value: element.group.id,
          text: element. group.title
        }));
        
        });
        $('#idgr').formSelect();
        $('#idgrdiv').show()
       // topic = data
        //#idless
      })
      
    })

    $('#idtopic').change(function(){
      console.log($('#idtopic').val());
      $('#idless').html("")
      $('#idless').append($('<option>', {
        value: -1,
        text: "Уроки",
        disabled:1,
        selected:1
      }));
      topic[$('#idtopic').val()].lessons.forEach(elment =>{
        less[elment.id] = elment
        $('#idless').append($('<option>', {
          value: elment.id,
          text: elment.title
        }));
      })

        $('#idless').formSelect();
        $('#idlessdiv').show()
    })

    $('#idless').change(function(){
      $('#idmatdiv').show()
      console.log(less[$('#idless').val()]);
      
      console.log($('#idless').val());
      $('#idmat').html("")
      $('#idmat').append($('<option>', {
        value: -1,
        text: "Новый материал",
        selected:1
      }));
      less[$('#idless').val()].steps.forEach(el =>{
        console.log(el);
        $('#idmat').append($('<option>', {
          value: el.id,
          text: el.title,
        }));
      })
      
      $('#idmat').formSelect();
      $('#idgrdiv').show()
    })

    $('#idgr').change(function(){
      $('#idkabdiv').show()
    })

    $('#idkab').change(function(){
      hidecontrol()
      console.dir($('#idkab').val());
      
      $.get( `/sendcmd`, { host:getkab(), cmd:"fullstat"} ).done(function( data ) {
        data = JSON.parse(data)
      if (data[0].outputActive) {
        $('#startrec').hide()
        $('#stoprec').show()
      } else {
        $('#startrec').show()
        $('#stoprec').hide()
      }
      if (data[1].outputActive) {
        $('#streamstart').hide()
        $('#streamstop').show()
      } else {
        $('#streamstart').show()
        $('#streamstop').hide()
      }
        console.log(data);
      })
    })
    function getkab (f=null) {
      let host = $('#idkab').val()
      host = host.split('_');
      if (f!=null){
        return host[0]
      }

      if (host[1]=='1') {host='127.0.0.1'}
      return host[1]
    }*/
  });


 