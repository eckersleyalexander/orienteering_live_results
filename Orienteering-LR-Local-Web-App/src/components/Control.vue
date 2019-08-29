<template>
    <div class="control">
        <h1>Control panel</h1>
        <b-alert v-model="offline" variant="danger">Not Connected</b-alert>
        <b-container fluid>
            <b-row>
                <b-col cols="4">
                    <b-card no-body header="Devices" >
                        <b-list-group flush>
                            <b-list-group-item v-for="client in leaderboards" 
                            :key="client.id" 
                            :active="client.id === selected"
                            v-on:click="selected = client.id">
                                {{client.id}}
                            </b-list-group-item>
                        </b-list-group>
                        <small class="d-block" slot="footer">{{leaderboards.length}} leaderboards and {{controls.length}} controls connected</small>
                    </b-card>
                </b-col>
                <b-col :hidden="selected == null">
                    <h3>{{selected}} settings</h3>
                    <div>
                        <b-form-select v-model="selectedClass" :options="classes"></b-form-select>
                        <div class="mt-3">Selected: <strong>{{ selectedClass }}</strong></div>
                    </div>
                </b-col>
            </b-row>
        </b-container>
    </div>
</template>

<script>
import {formatDistance} from "date-fns";
export default {
    name: "Control",
    data() {
        return {
            loading: true,
            selected: null,
            selectedClass: "<select a class>"
        };
    },
    computed: {
        offline() {return !this.$store.state.control.socket.online},
        leaderboards() {return this.$store.state.control.socket.clients.filter(client => client.type === "leaderboard")},
        controls() {return this.$store.state.control.socket.clients.filter(client => client.type === "control")},
        classes() {return this.$store.state.control.socket.classes.map(e => e.text = e.Name)},

    },
    created() {
        // store.actions.broadcast = data => this.onmessage(data);
    },
    methods: {
        
    }
};
</script>
