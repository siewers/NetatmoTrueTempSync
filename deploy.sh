#!/usr/bin/expect -f

set install_dir "/opt/netatmo-truetempsync"
set publish_dir "NetatmoTrueTempSync/bin/publish/linux-arm64"

send_user "Host: "
expect_user -re "(.*)\n" { set host $expect_out(1,string) }
send_user "Username: "
expect_user -re "(.*)\n" { set user $expect_out(1,string) }
stty -echo
send_user "Password: "
expect_user -re "(.*)\n" { set pass $expect_out(1,string) }
stty echo
send_user "\n"

send_user "Publishing for linux-arm64...\n"
if {[catch {exec dotnet publish NetatmoTrueTempSync -p:PublishProfile=linux-arm64 >@stdout 2>@stderr}]} {
    send_user "Publish failed.\n"
    exit 1
}

proc run_ssh {user host pass args} {
    spawn ssh -o PubkeyAuthentication=no "$user@$host" {*}$args
    expect "password:" { send "$pass\r" }
    expect eof
    lassign [wait] pid spawnid os_error exit_code
    return $exit_code
}

proc run_scp {user host pass src dst} {
    spawn scp -o PubkeyAuthentication=no $src "$user@$host:$dst"
    expect "password:" { send "$pass\r" }
    expect eof
    lassign [wait] pid spawnid os_error exit_code
    return $exit_code
}

send_user "Deploying to $user@$host:$install_dir...\n"
run_ssh $user $host $pass "sudo mkdir -p $install_dir && sudo chown $user:$user $install_dir"
run_scp $user $host $pass "$publish_dir/NetatmoTrueTempSync" "$install_dir/"
run_ssh $user $host $pass "$install_dir/NetatmoTrueTempSync service install"

send_user "Done!\n"
